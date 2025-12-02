using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Options;
using Polly;
using TradingApp.Contracts.Events;
using TradingApp.Kafka.Abstractions;
using TradingApp.MarketData.Ingestor.Application.Abstractions;
using TradingApp.MarketData.Ingestor.Application.Configuration;
using TradingApp.MarketData.Ingestor.Domain.Models;

namespace TradingApp.MarketData.Ingestor.Application.Services;

public sealed class TradeIngestionPipeline : ITradeIngestionPipeline
{
    private readonly IKafkaProducer<string, RawMarketTradeEvent> _producer;
    private readonly MarketDataIngestorOptions _options;
    private readonly ILogger<TradeIngestionPipeline> _logger;
    private readonly KafkaResilienceOptions _kafkaResilienceOptions;
    private readonly AsyncPolicy _kafkaPolicy;
    private readonly TimeSpan _throughputLogInterval;
    private readonly Stopwatch _throughputStopwatch;
    private readonly object _throughputLock = new();
    private long _messagesSinceLastLog;

    public TradeIngestionPipeline(
        IKafkaProducer<string, RawMarketTradeEvent> producer,
        IOptions<MarketDataIngestorOptions> options,
        IOptions<KafkaResilienceOptions> kafkaResilienceOptions,
        ILogger<TradeIngestionPipeline> logger)
    {
        _producer = producer;
        _options = options.Value;
        _kafkaResilienceOptions = kafkaResilienceOptions.Value;
        _logger = logger;
        _kafkaPolicy = BuildKafkaPolicy();
        var intervalSeconds = Math.Max(1, _options.ThroughputLogIntervalSeconds);
        _throughputLogInterval = TimeSpan.FromSeconds(intervalSeconds);
        _throughputStopwatch = Stopwatch.StartNew();
    }

    public async Task HandleAsync(BinanceCombinedTradeMessage message, CancellationToken cancellationToken)
    {
        var tradeEvent = MapToContract(message);
        var context = new Context($"KafkaProduce-{tradeEvent.Symbol}")
        {
            ["Symbol"] = tradeEvent.Symbol,
            ["TradeId"] = tradeEvent.TradeId
        };

        try
        {
            await _kafkaPolicy.ExecuteAsync(
                async (_, ct) =>
                {
                    await _producer.ProduceAsync(_options.KafkaTopic, tradeEvent.Symbol, tradeEvent, ct)
                        .ConfigureAwait(false);
                    TrackThroughput();
                },
                context,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to deliver trade {TradeId} for {Symbol} after retries. Routing to retry/DLQ.",
                tradeEvent.TradeId,
                tradeEvent.Symbol);

            await HandleKafkaFailureAsync(tradeEvent, cancellationToken).ConfigureAwait(false);
        }
    }

    private RawMarketTradeEvent MapToContract(BinanceCombinedTradeMessage message)
    {
        var data = message.Data;

        try
        {
            var tradeTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(data.TradeTime).UtcDateTime;
            var eventTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(data.EventTime).UtcDateTime;

            return new RawMarketTradeEvent(
                Guid.NewGuid().ToString("N"),
                "binance",
                data.Symbol,
                message.Stream,
                ParseDecimal(data.Price),
                ParseDecimal(data.Quantity),
                data.TradeId,
                data.IsMarketMaker,
                tradeTimeUtc,
                eventTimeUtc,
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to transform Binance trade message for symbol {Symbol}",
                data.Symbol);
            throw;
        }
    }

    private static decimal ParseDecimal(string value) =>
        decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    private AsyncPolicy BuildKafkaPolicy()
    {
        if (!_kafkaResilienceOptions.Enabled)
        {
            return Policy.NoOpAsync();
        }

        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                _kafkaResilienceOptions.MaxRetryAttempts,
                attempt =>
                {
                    var delay = _kafkaResilienceOptions.BaseDelayMilliseconds * Math.Pow(2, attempt - 1);
                    var jitter = _kafkaResilienceOptions.JitterMilliseconds > 0
                        ? Random.Shared.Next(0, _kafkaResilienceOptions.JitterMilliseconds + 1)
                        : 0;
                    var capped = Math.Min(delay + jitter, _kafkaResilienceOptions.MaxDelayMilliseconds);
                    return TimeSpan.FromMilliseconds(capped);
                },
                (exception, delay, attempt, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Kafka produce attempt {Attempt} failed. Retrying in {Delay}ms (Symbol={Symbol}, TradeId={TradeId})",
                        attempt,
                        delay.TotalMilliseconds,
                        context.ContainsKey("Symbol") ? context["Symbol"] : "?",
                        context.ContainsKey("TradeId") ? context["TradeId"] : "?");
                });
    }

    private async Task HandleKafkaFailureAsync(RawMarketTradeEvent tradeEvent, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.RetryTopic))
        {
            var retrySent = await TryProduceAsync(_options.RetryTopic!, tradeEvent, cancellationToken).ConfigureAwait(false);
            if (retrySent)
            {
                _logger.LogWarning(
                    "Enqueued trade {TradeId} for {Symbol} to retry topic {RetryTopic}",
                    tradeEvent.TradeId,
                    tradeEvent.Symbol,
                    _options.RetryTopic);
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(_options.DeadLetterTopic))
        {
            var dlqSent = await TryProduceAsync(_options.DeadLetterTopic!, tradeEvent, cancellationToken).ConfigureAwait(false);
            if (dlqSent)
            {
                _logger.LogError(
                    "Routed trade {TradeId} for {Symbol} to DLQ {DeadLetterTopic}",
                    tradeEvent.TradeId,
                    tradeEvent.Symbol,
                    _options.DeadLetterTopic);
                return;
            }
        }

        _logger.LogCritical(
            "Unable to persist trade {TradeId} for {Symbol} to Kafka (main, retry, or DLQ). Event dropped.",
            tradeEvent.TradeId,
            tradeEvent.Symbol);
    }

    private async Task<bool> TryProduceAsync(string topic, RawMarketTradeEvent tradeEvent, CancellationToken cancellationToken)
    {
        try
        {
            await _producer.ProduceAsync(topic, tradeEvent.Symbol, tradeEvent, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to produce trade {TradeId} for {Symbol} to topic {Topic}",
                tradeEvent.TradeId,
                tradeEvent.Symbol,
                topic);
            return false;
        }
    }

    private void TrackThroughput()
    {
        Interlocked.Increment(ref _messagesSinceLastLog);

        if (_throughputStopwatch.Elapsed < _throughputLogInterval)
        {
            return;
        }

        lock (_throughputLock)
        {
            if (_throughputStopwatch.Elapsed < _throughputLogInterval)
            {
                return;
            }

            var elapsed = _throughputStopwatch.Elapsed;
            _throughputStopwatch.Restart();

            var processed = Interlocked.Exchange(ref _messagesSinceLastLog, 0);
            if (processed == 0)
            {
                return;
            }

            var rate = processed / Math.Max(0.001, elapsed.TotalSeconds);
            _logger.LogInformation(
                "Processed {Count} trades in {Seconds:F1}s (~{Rate:F1} msg/s) -> Kafka topic {Topic}",
                processed,
                elapsed.TotalSeconds,
                rate,
                _options.KafkaTopic);
        }
    }
}

