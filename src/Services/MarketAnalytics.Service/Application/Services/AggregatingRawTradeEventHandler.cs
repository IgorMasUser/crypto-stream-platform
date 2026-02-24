using Microsoft.Extensions.Logging;
using TradingApp.Contracts.Events;
using TradingApp.Kafka.Abstractions;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;
using TradingApp.MarketAnalytics.Service.Domain;

namespace TradingApp.MarketAnalytics.Service.Application.Services;

/// <summary>
/// Minimal test aggregator: converts each raw trade into a single AggregatedMarketAnalyticsEvent
/// so we can see data flowing end-to-end. Not a real aggregation.
/// </summary>
public sealed class AggregatingRawTradeEventHandler : IRawTradeEventHandler
{
    private readonly IKafkaProducer<string, AggregatedMarketAnalyticsEvent> producer;
    private readonly ITradesAggregatorService tradesAggregatorService;
    private readonly ILogger<AggregatingRawTradeEventHandler> logger;
    private const string AggregatedTopic = "aggregated-market-analytics";
    private const int aggragationRangeinMinutes = 1;

    public AggregatingRawTradeEventHandler(
        IKafkaProducer<string, AggregatedMarketAnalyticsEvent> producer,
        ITradesAggregatorService tradesAggregatorService,
        ILogger<AggregatingRawTradeEventHandler> logger)
    {
        this.producer = producer;
        this.tradesAggregatorService = tradesAggregatorService;
        this.logger = logger;
    }

    public async Task HandleAsync(RawMarketTradeEvent trade, CancellationToken cancellationToken)
    {
        // Normalize to the start of the minute so all trades in that minute share the same key.
        var windowStart = new DateTime(
            trade.EventTimeUtc.Year,
            trade.EventTimeUtc.Month,
            trade.EventTimeUtc.Day,
            trade.EventTimeUtc.Hour,
            trade.EventTimeUtc.Minute,
            0,
            DateTimeKind.Utc);

        var aggregate = await this.tradesAggregatorService.BuildAggregatedTradesAsync(windowStart, aggragationRangeinMinutes, trade, cancellationToken);

        var key = $"{trade.Symbol}|{windowStart:O}";

        var aggregatedEvent = this.ToEvent(aggregate);

        await this.producer.ProduceAsync(AggregatedTopic, key, aggregatedEvent, cancellationToken)
            .ConfigureAwait(false);

        this.logger.LogInformation(
            "Published test aggregate for {Symbol} trade {TradeId} to topic {Topic} with key {Key}",
            aggregatedEvent.Symbol,
            trade.TradeId,
            AggregatedTopic,
            key);
    }

    private AggregatedMarketAnalyticsEvent ToEvent(AggregatedMarketAnalyticsEntity aggregate)
    {
        return new AggregatedMarketAnalyticsEvent(
            aggregate.Symbol,
            aggregate.WindowStartUtc,
            aggregate.WindowEndUtc,
            aggregate.OpenPrice,
            aggregate.HighPrice,
            aggregate.LowPrice,
            aggregate.LastPrice,
            aggregate.Volume,
            aggregate.TradesCount,
            aggregate.CreatedAtUtc);
    }
}

