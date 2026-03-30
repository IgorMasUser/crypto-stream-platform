using Microsoft.Extensions.Logging;
using TradingApp.Contracts.Events;
using TradingApp.Kafka.Abstractions;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;
using TradingApp.MarketAnalytics.Service.Domain;

namespace TradingApp.MarketAnalytics.Service.Application.Services;

public sealed class AggregatingRawTradeEventHandler : IRawTradeEventHandler
{
    private readonly IKafkaProducer<string, AggregatedMarketAnalyticsEvent> producer;
    private readonly ITradesAggregatorService tradesAggregatorService;
    private readonly ILogger<AggregatingRawTradeEventHandler> logger;
    private const string AggregatedTopic = "aggregated-market-analytics";
    private const int AggregationRangeInMinutes = 1;
    private const int MaxProduceRetries = 3;

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

        var aggregate = await this.tradesAggregatorService
            .BuildAggregatedTradesAsync(windowStart, AggregationRangeInMinutes, trade, cancellationToken);

        var key            = $"{trade.Symbol}|{windowStart:O}";
        var aggregatedEvent = this.ToEvent(aggregate);

        await this.ProduceWithRetryAsync(key, aggregatedEvent, trade.TradeId.ToString(), cancellationToken);
    }

    private async Task ProduceWithRetryAsync(
        string key,
        AggregatedMarketAnalyticsEvent aggregatedEvent,
        string tradeId,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                await this.producer
                    .ProduceAsync(AggregatedTopic, key, aggregatedEvent, cancellationToken)
                    .ConfigureAwait(false);

                this.logger.LogDebug(
                    "Published aggregate for {Symbol} trade {TradeId} key={Key}",
                    aggregatedEvent.Symbol, tradeId, key);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxProduceRetries)
            {
                attempt++;
                var delay = TimeSpan.FromMilliseconds(200 * (1 << attempt)); // 400 → 800 → 1600 ms
                this.logger.LogWarning(
                    ex,
                    "ProduceAsync failed (attempt {Attempt}/{MaxRetries}), retrying in {DelayMs}ms for key {Key}",
                    attempt, MaxProduceRetries, delay.TotalMilliseconds, key);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogError(
                    ex,
                    "ProduceAsync failed after {MaxRetries} retries for key {Key} — trade dropped",
                    MaxProduceRetries, key);
                throw;
            }
        }
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
