using Microsoft.EntityFrameworkCore;
using Npgsql;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;
using TradingApp.MarketAnalytics.Service.Domain;

namespace TradingApp.MarketAnalytics.Service.Infrastructure.Persistence;

public sealed class MarketAnalyticsRepository : IMarketAnalyticsRepository
{
    private readonly IDbContextFactory<AnalyticsDbContext> dbFactory;

    public MarketAnalyticsRepository(IDbContextFactory<AnalyticsDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    public async Task<AggregatedMarketAnalyticsEntity> UpsertWindowAsync(
        string symbol,
        DateTime windowStart,
        DateTime windowEnd,
        Func<AggregatedMarketAnalyticsEntity> createNew,
        Action<AggregatedMarketAnalyticsEntity> applyUpdate,
        CancellationToken cancellationToken = default)
    {
        await using var context = await this.dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await context.Aggregates
            .FindAsync([symbol, windowStart], cancellationToken);

        if (existing is null)
        {
            var newEntity = createNew();
            context.Aggregates.Add(newEntity);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return newEntity;
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // A concurrent replica inserted the same window between our FindAsync and
                // SaveChangesAsync. Reload the now-existing row and apply the update instead.
                context.ChangeTracker.Clear();
                existing = await context.Aggregates
                    .FindAsync([symbol, windowStart], cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Row for {symbol}|{windowStart:O} disappeared after unique constraint violation.");
            }
        }

        applyUpdate(existing);
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg && pg.SqlState == "23505";
}
