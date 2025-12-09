using TradingApp.Contracts.Events;
using TradingApp.Kafka.Abstractions;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;

namespace TradingApp.MarketAnalytics.Service.Application.Handlers;

/// <summary>
/// Minimal test aggregator: converts each raw trade into a single AggregatedMarketAnalyticsEvent
/// so we can see data flowing end-to-end. Not a real aggregation.
/// </summary>
public sealed class AggregatingRawTradeEventHandler : IRawTradeEventHandler
{
    private readonly IKafkaProducer<string, AggregatedMarketAnalyticsEvent> _producer;
    private readonly ILogger<AggregatingRawTradeEventHandler> _logger;
    private const string AggregatedTopic = "aggregated-market-analytics";

    public AggregatingRawTradeEventHandler(
        IKafkaProducer<string, AggregatedMarketAnalyticsEvent> producer,
        ILogger<AggregatingRawTradeEventHandler> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task HandleAsync(RawMarketTradeEvent trade, CancellationToken cancellationToken)
    {
        // Create a tiny 1-minute window around the trade just for demo purposes.
        var windowEnd = trade.EventTimeUtc;
        var windowStart = trade.EventTimeUtc.AddMinutes(-1);

        var aggregated = new AggregatedMarketAnalyticsEvent(
            EventId: Guid.NewGuid().ToString("N"),
            Symbol: trade.Symbol,
            WindowStartUtc: windowStart,
            WindowEndUtc: windowEnd,
            OpenPrice: trade.Price,
            HighPrice: trade.Price,
            LowPrice: trade.Price,
            LastPrice: trade.Price,
            Volume: trade.Quantity,
            TradesCount: 1,
            CreatedAtUtc: DateTime.UtcNow);

        await _producer.ProduceAsync(AggregatedTopic, trade.Symbol, aggregated, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Published test aggregate for {Symbol} trade {TradeId} to topic {Topic}",
            trade.Symbol,
            trade.TradeId,
            AggregatedTopic);
    }
}

