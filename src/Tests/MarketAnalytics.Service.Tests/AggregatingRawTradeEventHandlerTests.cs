using Microsoft.Extensions.Logging;
using NSubstitute;
using TradingApp.Contracts.Events;
using TradingApp.Kafka.Abstractions;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;
using TradingApp.MarketAnalytics.Service.Application.Services;
using TradingApp.MarketAnalytics.Service.Domain;

namespace MarketAnalytics.Service.Tests;

public sealed class AggregatingRawTradeEventHandlerTests
{
    private readonly IKafkaProducer<string, AggregatedMarketAnalyticsEvent> _producer =
        Substitute.For<IKafkaProducer<string, AggregatedMarketAnalyticsEvent>>();

    private readonly ITradesAggregatorService _aggregator =
        Substitute.For<ITradesAggregatorService>();

    private readonly ILogger<AggregatingRawTradeEventHandler> _logger =
        Substitute.For<ILogger<AggregatingRawTradeEventHandler>>();

    private readonly AggregatingRawTradeEventHandler _sut;

    public AggregatingRawTradeEventHandlerTests()
    {
        _sut = new AggregatingRawTradeEventHandler(_producer, _aggregator, _logger);

        // default: aggregator returns a valid entity
        _aggregator
            .BuildAggregatedTradesAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<RawMarketTradeEvent>())
            .Returns(ci => MakeEntity(ci.ArgAt<DateTime>(0), ci.ArgAt<RawMarketTradeEvent>(2)));
    }

    // ── window start normalization ─────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_TradeAtArbitrarySecond_WindowStartNormalizedToMinute()
    {
        var tradeTime = new DateTime(2026, 3, 19, 15, 47, 32, DateTimeKind.Utc);
        var trade = MakeTrade("BTCUSDT", tradeTime);

        await _sut.HandleAsync(trade, CancellationToken.None);

        var expectedWindow = new DateTime(2026, 3, 19, 15, 47, 0, DateTimeKind.Utc);
        await _aggregator.Received(1)
            .BuildAggregatedTradesAsync(expectedWindow, Arg.Any<int>(), trade);
    }

    [Fact]
    public async Task HandleAsync_TradeAtExactMinute_WindowStartIsTheSameMinute()
    {
        var tradeTime = new DateTime(2026, 3, 19, 15, 47, 0, DateTimeKind.Utc);
        var trade = MakeTrade("BTCUSDT", tradeTime);

        await _sut.HandleAsync(trade, CancellationToken.None);

        await _aggregator.Received(1)
            .BuildAggregatedTradesAsync(tradeTime, Arg.Any<int>(), trade);
    }

    // ── Kafka key format ──────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ProducesCorrectKey_SymbolPipeWindowStart()
    {
        var tradeTime = new DateTime(2026, 3, 19, 16, 0, 45, DateTimeKind.Utc);
        var trade = MakeTrade("ETHUSDT", tradeTime);
        string? capturedKey = null;

        await _producer.ProduceAsync(
            Arg.Any<string>(),
            Arg.Do<string>(k => capturedKey = k),
            Arg.Any<AggregatedMarketAnalyticsEvent>());

        await _sut.HandleAsync(trade, CancellationToken.None);

        Assert.Equal("ETHUSDT|2026-03-19T16:00:00.0000000Z", capturedKey);
    }

    // ── target topic ──────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ProducesToAggregatedMarketAnalyticsTopic()
    {
        var trade = MakeTrade("BTCUSDT", DateTime.UtcNow);
        string? capturedTopic = null;

        await _producer.ProduceAsync(
            Arg.Do<string>(t => capturedTopic = t),
            Arg.Any<string>(),
            Arg.Any<AggregatedMarketAnalyticsEvent>());

        await _sut.HandleAsync(trade, CancellationToken.None);

        Assert.Equal("aggregated-market-analytics", capturedTopic);
    }

    // ── aggregator is always called ───────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Always_CallsAggregatorService()
    {
        var trade = MakeTrade("SOLUSDT", DateTime.UtcNow);

        await _sut.HandleAsync(trade, CancellationToken.None);

        await _aggregator.Received(1)
            .BuildAggregatedTradesAsync(Arg.Any<DateTime>(), 1, trade);
    }

    // ── event mapping ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ProducedEvent_HasSameSymbolAndPricesAsAggregate()
    {
        var tradeTime = DateTime.UtcNow;
        var trade = MakeTrade("XRPUSDT", tradeTime, price: 0.55m);

        AggregatedMarketAnalyticsEvent? captured = null;
        await _producer.ProduceAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Do<AggregatedMarketAnalyticsEvent>(e => captured = e));

        await _sut.HandleAsync(trade, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("XRPUSDT", captured!.Symbol);
        Assert.Equal(0.55m, captured.OpenPrice);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static RawMarketTradeEvent MakeTrade(
        string symbol,
        DateTime eventTime,
        decimal price = 100m) =>
        new("evt-1", "binance", symbol, $"{symbol.ToLower()}@trade",
            price, 1m, 1L, false,
            eventTime, eventTime, eventTime);

    private static AggregatedMarketAnalyticsEntity MakeEntity(
        DateTime windowStart,
        RawMarketTradeEvent trade) =>
        new()
        {
            Symbol         = trade.Symbol,
            WindowStartUtc = windowStart,
            WindowEndUtc   = windowStart.AddMinutes(1),
            OpenPrice      = trade.Price,
            HighPrice      = trade.Price,
            LowPrice       = trade.Price,
            LastPrice      = trade.Price,
            Volume         = trade.Quantity,
            TradesCount    = 1,
            CreatedAtUtc   = DateTime.UtcNow,
            UpdatedAtUtc   = DateTime.UtcNow,
        };
}
