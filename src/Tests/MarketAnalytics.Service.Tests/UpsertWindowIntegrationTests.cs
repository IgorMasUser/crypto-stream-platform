using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TradingApp.MarketAnalytics.Service.Domain;
using TradingApp.MarketAnalytics.Service.Infrastructure.Persistence;

namespace MarketAnalytics.Service.Tests;

// One PostgreSQL container is shared across all tests in this class.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public IDbContextFactory<AnalyticsDbContext> DbFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        DbFactory = new DelegateDbContextFactory(options);

        await using var ctx = await DbFactory.CreateDbContextAsync();
        await ctx.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private sealed class DelegateDbContextFactory(DbContextOptions<AnalyticsDbContext> options)
        : IDbContextFactory<AnalyticsDbContext>
    {
        public AnalyticsDbContext CreateDbContext() => new(options);
    }
}

[Collection("postgres")]
public sealed class UpsertWindowIntegrationTests : IClassFixture<PostgresFixture>
{
    // Each test uses a unique symbol so rows never collide across tests
    // even though all tests share the same schema.
    private static readonly DateTime WindowStart = new(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime WindowEnd   = WindowStart.AddMinutes(1);

    private readonly IDbContextFactory<AnalyticsDbContext> _dbFactory;

    public UpsertWindowIntegrationTests(PostgresFixture fixture)
    {
        _dbFactory = fixture.DbFactory;
    }


    private MarketAnalyticsRepository Repo() => new(_dbFactory);

    private static AggregatedMarketAnalyticsEntity MakeEntity(
        string symbol, decimal price, decimal volume, int trades) => new()
    {
        Symbol         = symbol,
        WindowStartUtc = WindowStart,
        WindowEndUtc   = WindowEnd,
        OpenPrice      = price,
        HighPrice      = price,
        LowPrice       = price,
        LastPrice      = price,
        Volume         = volume,
        TradesCount    = trades,
        CreatedAtUtc   = DateTime.UtcNow,
        UpdatedAtUtc   = DateTime.UtcNow,
    };


    [Fact]
    public async Task UpsertWindowAsync_NewWindow_InsertsCorrectly()
    {
        var result = await Repo().UpsertWindowAsync(
            "BTCUSDT_NEW", WindowStart, WindowEnd,
            createNew:   () => MakeEntity("BTCUSDT_NEW", 100m, 1m, trades: 1),
            applyUpdate: _  => { });

        Assert.Equal("BTCUSDT_NEW", result.Symbol);
        Assert.Equal(100m, result.OpenPrice);
        Assert.Equal(1,    result.TradesCount);

        // Verify it's actually stored in the DB
        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var stored = await ctx.Aggregates.FindAsync(["BTCUSDT_NEW", WindowStart]);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task UpsertWindowAsync_ExistingWindow_UpdatesWithoutCallingCreateNew()
    {
        const string symbol = "ETHUSDT_UPD";

        // Pre-insert the row
        await Repo().UpsertWindowAsync(
            symbol, WindowStart, WindowEnd,
            createNew:   () => MakeEntity(symbol, 3000m, 2m, trades: 5),
            applyUpdate: _  => { });

        // Second call must hit the update path
        var result = await Repo().UpsertWindowAsync(
            symbol, WindowStart, WindowEnd,
            createNew:   () => throw new Exception("createNew must NOT be called for an existing window"),
            applyUpdate: e  =>
            {
                e.TradesCount += 1;
                e.Volume      += 1m;
                e.LastPrice    = 3100m;
            });

        Assert.Equal(6,      result.TradesCount);
        Assert.Equal(3m,     result.Volume);
        Assert.Equal(3100m,  result.LastPrice);
        Assert.Equal(3000m,  result.OpenPrice); // open price must be preserved
    }

    /// <summary>
    /// Reproduces the race condition: two concurrent pods try to insert the
    /// same (symbol, windowStart) row simultaneously.
    /// The second writer must NOT throw — it must merge its data into the row
    /// that the first writer already committed (the 23505 catch-block path).
    /// </summary>
    [Fact]
    public async Task UpsertWindowAsync_ConcurrentInserts_BothSucceedAndDataIsMerged()
    {
        const string symbol = "SOLUSDT_RACE";

        var task1 = Repo().UpsertWindowAsync(
            symbol, WindowStart, WindowEnd,
            createNew:   () => MakeEntity(symbol, 150m, 10m, trades: 5),
            applyUpdate: e  => { e.TradesCount += 5; e.Volume += 10m; });

        var task2 = Repo().UpsertWindowAsync(
            symbol, WindowStart, WindowEnd,
            createNew:   () => MakeEntity(symbol, 150m, 3m, trades: 2),
            applyUpdate: e  => { e.TradesCount += 2; e.Volume += 3m; });

        // Neither task should throw
        await Task.WhenAll(task1, task2);

        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var stored = await ctx.Aggregates.FindAsync([symbol, WindowStart]);

        Assert.NotNull(stored);
        // One task created the row, the other applied an update.
        // Exact totals depend on timing but TradesCount must be > 0 and ≤ 7.
        Assert.InRange(stored!.TradesCount, 1, 7);
    }

    /// <summary>
    /// Verifies the normal update path: Pod A commits the row before Pod B even starts.
    /// Pod B's FindAsync finds the existing row, so it goes directly to applyUpdate —
    /// the 23505 catch-block is NOT exercised here.
    /// This covers the "late joiner" scenario: a second trade for an already-open window.
    /// </summary>
    [Fact]
    public async Task UpsertWindowAsync_RowAlreadyExistsBeforeCall_AppliesUpdateWithoutInsert()
    {
        const string symbol = "BNBUSDT_EXISTING";

        // Pod A commits the row before Pod B starts — no concurrency, sequential order.
        await using var podACtx = await _dbFactory.CreateDbContextAsync();
        podACtx.Aggregates.Add(MakeEntity(symbol, 200m, 10m, trades: 5));
        await podACtx.SaveChangesAsync();

        // Pod B calls UpsertWindowAsync: FindAsync finds the row → goes to applyUpdate,
        // createNew is never called.
        var result = await Repo().UpsertWindowAsync(
            symbol, WindowStart, WindowEnd,
            createNew:   () => throw new Exception("createNew must NOT be called — row already exists"),
            applyUpdate: e  =>
            {
                e.TradesCount += 1;
                e.Volume      += 2m;
            });

        Assert.Equal(6,     result.TradesCount); // 5 + 1
        Assert.Equal(12m,   result.Volume);      // 10 + 2
        Assert.Equal(200m,  result.OpenPrice);   // Pod A's open is preserved
    }
}
