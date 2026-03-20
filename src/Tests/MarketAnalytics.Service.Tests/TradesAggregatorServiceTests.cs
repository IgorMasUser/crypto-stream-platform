using Microsoft.Extensions.Logging;
using NSubstitute;
using TradingApp.Contracts.Events;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;
using TradingApp.MarketAnalytics.Service.Application.Services;
using TradingApp.MarketAnalytics.Service.Domain;

namespace MarketAnalytics.Service.Tests;

public sealed class TradesAggregatorServiceTests
{
    private readonly IMarketAnalyticsRepository _repository = Substitute.For<IMarketAnalyticsRepository>();
    private readonly ILogger<TradesAggregatorService> _logger = Substitute.For<ILogger<TradesAggregatorService>>();
    private readonly TradesAggregatorService _sut;

    private static readonly DateTime WindowStart = new(2026, 3, 19, 16, 0, 0, DateTimeKind.Utc);

    public TradesAggregatorServiceTests()
    {
        _sut = new TradesAggregatorService(_repository, _logger);
    }

    // ── null guard ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildAggregatedTradesAsync_NullTrade_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.BuildAggregatedTradesAsync(WindowStart, 1, null!, CancellationToken.None));
    }

    // ── new window (first trade) ───────────────────────────────────────────────

    [Fact]
    public async Task BuildAggregatedTradesAsync_NoExistingWindow_SetsOhlcvFromFirstTrade()
    {
        var trade = MakeTrade(price: 100m, qty: 5m);
        SetupUpsertAsNewWindow();

        var result = await _sut.BuildAggregatedTradesAsync(WindowStart, 1, trade);

        Assert.Equal(trade.Symbol, result.Symbol);
        Assert.Equal(100m, result.OpenPrice);
        Assert.Equal(100m, result.HighPrice);
        Assert.Equal(100m, result.LowPrice);
        Assert.Equal(100m, result.LastPrice);
        Assert.Equal(5m,   result.Volume);
        Assert.Equal(1,    result.TradesCount);
        Assert.Equal(WindowStart, result.WindowStartUtc);
        Assert.Equal(WindowStart.AddMinutes(1), result.WindowEndUtc);
    }

    [Fact]
    public async Task BuildAggregatedTradesAsync_NoExistingWindow_CallsUpsertWindowAsync()
    {
        var trade = MakeTrade(price: 100m, qty: 1m);
        SetupUpsertAsNewWindow();

        await _sut.BuildAggregatedTradesAsync(WindowStart, 1, trade);

        await _repository.Received(1).UpsertWindowAsync(
            trade.Symbol,
            WindowStart,
            WindowStart.AddMinutes(1),
            Arg.Any<Func<AggregatedMarketAnalyticsEntity>>(),
            Arg.Any<Action<AggregatedMarketAnalyticsEntity>>());
    }

    // ── existing window (subsequent trades) ───────────────────────────────────

    [Fact]
    public async Task BuildAggregatedTradesAsync_ExistingWindow_UpdatesTradesCountAndVolume()
    {
        var existing = MakeEntity(open: 100m, high: 105m, low: 98m, last: 102m, volume: 10m, trades: 3);
        var trade = MakeTrade(price: 103m, qty: 2m);
        SetupUpsertAsExistingWindow(existing);

        var result = await _sut.BuildAggregatedTradesAsync(WindowStart, 1, trade);

        Assert.Equal(4,    result.TradesCount);
        Assert.Equal(12m,  result.Volume);
        Assert.Equal(103m, result.LastPrice);
    }

    [Fact]
    public async Task BuildAggregatedTradesAsync_TradeHigherThanExistingHigh_UpdatesHighPrice()
    {
        var existing = MakeEntity(open: 100m, high: 105m, low: 98m, last: 100m, volume: 1m, trades: 1);
        var trade = MakeTrade(price: 110m, qty: 1m);
        SetupUpsertAsExistingWindow(existing);

        var result = await _sut.BuildAggregatedTradesAsync(WindowStart, 1, trade);

        Assert.Equal(110m, result.HighPrice);
        Assert.Equal(98m,  result.LowPrice);
    }

    [Fact]
    public async Task BuildAggregatedTradesAsync_TradeLowerThanExistingLow_UpdatesLowPrice()
    {
        var existing = MakeEntity(open: 100m, high: 105m, low: 98m, last: 100m, volume: 1m, trades: 1);
        var trade = MakeTrade(price: 90m, qty: 1m);
        SetupUpsertAsExistingWindow(existing);

        var result = await _sut.BuildAggregatedTradesAsync(WindowStart, 1, trade);

        Assert.Equal(105m, result.HighPrice);
        Assert.Equal(90m,  result.LowPrice);
    }

    [Fact]
    public async Task BuildAggregatedTradesAsync_ExistingWindow_OpenPriceNotChanged()
    {
        var existing = MakeEntity(open: 100m, high: 105m, low: 98m, last: 100m, volume: 1m, trades: 1);
        var trade = MakeTrade(price: 200m, qty: 1m);
        SetupUpsertAsExistingWindow(existing);

        var result = await _sut.BuildAggregatedTradesAsync(WindowStart, 1, trade);

        Assert.Equal(100m, result.OpenPrice); // open must never change
    }

    // ── window end calculation ─────────────────────────────────────────────────

    [Fact]
    public async Task BuildAggregatedTradesAsync_AggregationRange_CorrectWindowEnd()
    {
        var trade = MakeTrade(price: 100m, qty: 1m);
        SetupUpsertAsNewWindow();

        var result = await _sut.BuildAggregatedTradesAsync(WindowStart, aggregationRangeInMinutes: 5, trade);

        Assert.Equal(WindowStart.AddMinutes(5), result.WindowEndUtc);
    }

    // ── mock helpers ───────────────────────────────────────────────────────────

    /// <summary>Simulates a new window — invokes the <c>createNew</c> callback.</summary>
    private void SetupUpsertAsNewWindow()
    {
        _repository
            .UpsertWindowAsync(
                Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<Func<AggregatedMarketAnalyticsEntity>>(),
                Arg.Any<Action<AggregatedMarketAnalyticsEntity>>())
            .Returns(callInfo =>
            {
                var createNew = callInfo.ArgAt<Func<AggregatedMarketAnalyticsEntity>>(3);
                return Task.FromResult(createNew());
            });
    }

    /// <summary>Simulates an existing window — invokes the <c>applyUpdate</c> callback on <paramref name="existing"/>.</summary>
    private void SetupUpsertAsExistingWindow(AggregatedMarketAnalyticsEntity existing)
    {
        _repository
            .UpsertWindowAsync(
                Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<Func<AggregatedMarketAnalyticsEntity>>(),
                Arg.Any<Action<AggregatedMarketAnalyticsEntity>>())
            .Returns(callInfo =>
            {
                var applyUpdate = callInfo.ArgAt<Action<AggregatedMarketAnalyticsEntity>>(4);
                applyUpdate(existing);
                return Task.FromResult(existing);
            });
    }

    // ── data builders ──────────────────────────────────────────────────────────

    private static RawMarketTradeEvent MakeTrade(decimal price, decimal qty) =>
        new("evt-1", "binance", "BTCUSDT", "btcusdt@trade",
            price, qty, 1L, false,
            WindowStart, WindowStart, WindowStart);

    private static AggregatedMarketAnalyticsEntity MakeEntity(
        decimal open, decimal high, decimal low, decimal last, decimal volume, int trades) =>
        new()
        {
            Symbol         = "BTCUSDT",
            WindowStartUtc = WindowStart,
            WindowEndUtc   = WindowStart.AddMinutes(1),
            OpenPrice      = open,
            HighPrice      = high,
            LowPrice       = low,
            LastPrice      = last,
            Volume         = volume,
            TradesCount    = trades,
            CreatedAtUtc   = DateTime.UtcNow,
            UpdatedAtUtc   = DateTime.UtcNow,
        };
}
