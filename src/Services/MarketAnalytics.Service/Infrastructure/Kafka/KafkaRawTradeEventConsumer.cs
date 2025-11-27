using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingApp.Contracts.Events;
using TradingApp.Kafka.Configuration;
using TradingApp.MarketAnalytics.Service.Application.Configuration;

namespace TradingApp.MarketAnalytics.Service.Infrastructure.Kafka;

public sealed class KafkaRawTradeEventConsumer : IRawTradeEventConsumer
{
    private readonly KafkaProducerOptions _producerOptions;
    private readonly KafkaTradeConsumerOptions _consumerOptions;
    private readonly ILogger<KafkaRawTradeEventConsumer> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public KafkaRawTradeEventConsumer(
        IOptions<KafkaProducerOptions> producerOptions,
        IOptions<KafkaTradeConsumerOptions> consumerOptions,
        ILogger<KafkaRawTradeEventConsumer> logger)
    {
        _producerOptions = producerOptions.Value;
        _consumerOptions = consumerOptions.Value;
        _logger = logger;
    }

    public async Task ConsumeAsync(
        Func<RawMarketTradeEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        if (!_consumerOptions.Enabled)
        {
            _logger.LogInformation("Kafka trade consumer is disabled via configuration.");
            return;
        }

        var bootstrapServers = !string.IsNullOrWhiteSpace(_producerOptions.BootstrapServers)
            ? _producerOptions.BootstrapServers
            : throw new InvalidOperationException("Kafka bootstrap servers must be configured.");

        if (string.IsNullOrWhiteSpace(_consumerOptions.Topic))
        {
            throw new InvalidOperationException("Kafka trade consumer topic is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_consumerOptions.GroupId))
        {
            throw new InvalidOperationException("Kafka trade consumer group id is not configured.");
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = _consumerOptions.GroupId,
            EnableAutoCommit = _consumerOptions.EnableAutoCommit,
            AutoOffsetReset = _consumerOptions.AutoOffsetReset,
            ClientId = $"{_producerOptions.ClientId ?? Environment.MachineName}-consumer",
            AllowAutoCreateTopics = false
        };

        if (_consumerOptions.StatisticsIntervalMs.HasValue)
        {
            config.StatisticsIntervalMs = _consumerOptions.StatisticsIntervalMs;
        }

        using var consumer = new ConsumerBuilder<string, RawMarketTradeEvent>(config)
            .SetValueDeserializer(new RawMarketTradeEventDeserializer(_jsonOptions))
            .SetErrorHandler((_, error) =>
            {
                if (error.IsFatal)
                {
                    _logger.LogError("Kafka consumer fatal error: {Reason}", error.Reason);
                }
                else
                {
                    _logger.LogWarning("Kafka consumer warning: {Reason}", error.Reason);
                }
            })
            .SetStatisticsHandler((_, stats) => _logger.LogDebug("Kafka consumer stats: {Stats}", stats))
            .Build();

        consumer.Subscribe(_consumerOptions.Topic);
        _logger.LogInformation(
            "Kafka trade consumer subscribed to {Topic} with group {GroupId}.",
            _consumerOptions.Topic,
            _consumerOptions.GroupId);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(cancellationToken);
                    if (result?.Message?.Value is null)
                    {
                        continue;
                    }

                    await handler(result.Message.Value, cancellationToken).ConfigureAwait(false);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume exception on topic {Topic}", _consumerOptions.Topic);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka trade consumer cancellation requested.");
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Kafka trade consumer closed.");
        }
    }

    private sealed class RawMarketTradeEventDeserializer : IDeserializer<RawMarketTradeEvent>
    {
        private readonly JsonSerializerOptions _options;

        public RawMarketTradeEventDeserializer(JsonSerializerOptions options)
        {
            _options = options;
        }

        public RawMarketTradeEvent Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (isNull || data.IsEmpty)
            {
                throw new InvalidOperationException("Kafka message payload was empty.");
            }

            return JsonSerializer.Deserialize<RawMarketTradeEvent>(data, _options)
                ?? throw new JsonException("Unable to deserialize RawMarketTradeEvent from Kafka payload.");
        }
    }
}


