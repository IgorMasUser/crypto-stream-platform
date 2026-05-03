using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using TradingApp.Contracts.Events;
using TradingApp.Kafka.Abstractions;
using TradingApp.Kafka.Configuration;
using TradingApp.MarketData.Ingestor.Application.Configuration;

namespace TradingApp.MarketData.Ingestor.Worker.HostedServices;

/// <summary>
/// Consumes messages from the retry topic, waits a configured delay,
/// then attempts to re-publish them to the main topic.
/// If re-publishing fails the message is routed to the DLQ so the
/// consumer offset can advance and no message blocks the pipeline forever.
/// </summary>
public sealed class RetryConsumerHostedService : BackgroundService
{
    private readonly IKafkaProducer<string, RawMarketTradeEvent> _producer;
    private readonly MarketDataIngestorOptions _options;
    private readonly ILogger<RetryConsumerHostedService> _logger;
    private readonly IConsumer<string, string> _consumer;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public RetryConsumerHostedService(
        IKafkaProducer<string, RawMarketTradeEvent> producer,
        IOptions<MarketDataIngestorOptions> options,
        IOptions<KafkaProducerOptions> kafkaOptions,
        ILogger<RetryConsumerHostedService> logger)
    {
        _producer = producer;
        _options  = options.Value;
        _logger   = logger;

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers      = kafkaOptions.Value.BootstrapServers,
            GroupId               = _options.RetryConsumer.GroupId,
            AutoOffsetReset       = AutoOffsetReset.Earliest,
            EnableAutoCommit      = true,
            EnableAutoOffsetStore = false,
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.RetryTopic))
        {
            _logger.LogInformation("RetryTopic is not configured — retry consumer is disabled");
            return;
        }

        _logger.LogInformation(
            "Retry consumer starting on topic {Topic} (delay={Delay}s, dlq={Dlq})",
            _options.RetryTopic,
            _options.RetryConsumer.RetryDelaySeconds,
            _options.DeadLetterTopic ?? "(none)");

        _consumer.Subscribe(_options.RetryTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = null;
            try
            {
                result = _consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null)
                    continue;

                var evt = JsonSerializer.Deserialize<RawMarketTradeEvent>(result.Message.Value, _jsonOptions);
                if (evt is null)
                {
                    _logger.LogWarning("Retry consumer: failed to deserialize message at offset {Offset}", result.Offset);
                    _consumer.StoreOffset(result);
                    continue;
                }

                _logger.LogInformation(
                    "Retry consumer: received {Symbol} TradeId={TradeId} — waiting {Delay}s before re-publish",
                    evt.Symbol, evt.TradeId, _options.RetryConsumer.RetryDelaySeconds);

                // Wait before retrying to give the broker time to recover
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.RetryConsumer.RetryDelaySeconds),
                    stoppingToken);

                await RepublishOrDlqAsync(evt, stoppingToken);

                _consumer.StoreOffset(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retry consumer: unexpected error, pausing 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _consumer.Close();
    }

    private async Task RepublishOrDlqAsync(RawMarketTradeEvent evt, CancellationToken cancellationToken)
    {
        try
        {
            await _producer.ProduceAsync(_options.KafkaTopic, evt.Symbol, evt, cancellationToken);

            _logger.LogInformation(
                "Retry consumer: re-published {Symbol} TradeId={TradeId} to {Topic}",
                evt.Symbol, evt.TradeId, _options.KafkaTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Retry consumer: re-publish failed for {Symbol} TradeId={TradeId} — routing to DLQ",
                evt.Symbol, evt.TradeId);

            await SendToDlqAsync(evt, cancellationToken);
        }
    }

    private async Task SendToDlqAsync(RawMarketTradeEvent evt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.DeadLetterTopic))
        {
            _logger.LogCritical(
                "Retry consumer: DLQ topic not configured — event dropped! Symbol={Symbol} TradeId={TradeId}",
                evt.Symbol, evt.TradeId);
            return;
        }

        try
        {
            await _producer.ProduceAsync(_options.DeadLetterTopic, evt.Symbol, evt, cancellationToken);

            _logger.LogError(
                "Retry consumer: event routed to DLQ {Dlq} Symbol={Symbol} TradeId={TradeId}",
                _options.DeadLetterTopic, evt.Symbol, evt.TradeId);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "Retry consumer: DLQ produce also failed — event permanently lost! Symbol={Symbol} TradeId={TradeId}",
                evt.Symbol, evt.TradeId);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _consumer.Dispose();
    }
}
