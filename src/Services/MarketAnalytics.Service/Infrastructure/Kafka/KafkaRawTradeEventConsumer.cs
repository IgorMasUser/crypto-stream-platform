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
    private readonly KafkaProducerOptions producerOptions;
    private readonly KafkaTradeConsumerOptions consumerOptions;
    private readonly ILogger<KafkaRawTradeEventConsumer> logger;
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    public KafkaRawTradeEventConsumer(
        IOptions<KafkaProducerOptions> producerOptions,
        IOptions<KafkaTradeConsumerOptions> consumerOptions,
        ILogger<KafkaRawTradeEventConsumer> logger)
    {
        this.producerOptions = producerOptions.Value;
        this.consumerOptions = consumerOptions.Value;
        this.logger          = logger;
    }

    public async Task ConsumeAsync(
        Func<RawMarketTradeEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        if (!this.consumerOptions.Enabled)
        {
            this.logger.LogInformation("Kafka trade consumer is disabled via configuration.");
            return;
        }

        var bootstrapServers = !string.IsNullOrWhiteSpace(this.producerOptions.BootstrapServers)
            ? this.producerOptions.BootstrapServers
            : throw new InvalidOperationException("Kafka bootstrap servers must be configured.");

        if (string.IsNullOrWhiteSpace(this.consumerOptions.Topic))
            throw new InvalidOperationException("Kafka trade consumer topic is not configured.");

        if (string.IsNullOrWhiteSpace(this.consumerOptions.GroupId))
            throw new InvalidOperationException("Kafka trade consumer group id is not configured.");

        var config = new ConsumerConfig
        {
            BootstrapServers    = bootstrapServers,
            GroupId             = this.consumerOptions.GroupId,
            EnableAutoCommit    = this.consumerOptions.EnableAutoCommit,
            AutoOffsetReset     = this.consumerOptions.AutoOffsetReset,
            ClientId            = $"{this.producerOptions.ClientId ?? Environment.MachineName}-consumer",
            AllowAutoCreateTopics = false
        };

        if (this.consumerOptions.StatisticsIntervalMs.HasValue)
            config.StatisticsIntervalMs = this.consumerOptions.StatisticsIntervalMs;

        using var consumer = new ConsumerBuilder<string, RawMarketTradeEvent>(config)
            .SetValueDeserializer(new RawMarketTradeEventDeserializer(this.jsonOptions))
            .SetErrorHandler((_, error) =>
            {
                if (error.IsFatal)
                    this.logger.LogError("Kafka consumer fatal error: {Reason}", error.Reason);
                else
                    this.logger.LogWarning("Kafka consumer warning: {Reason}", error.Reason);
            })
            .SetStatisticsHandler((_, stats) =>
                this.logger.LogDebug("Kafka consumer stats: {Stats}", stats))
            .Build();

        consumer.Subscribe(this.consumerOptions.Topic);
        this.logger.LogInformation(
            "Kafka trade consumer subscribed to {Topic} with group {GroupId}.",
            this.consumerOptions.Topic,
            this.consumerOptions.GroupId);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(cancellationToken);
                    if (result?.Message?.Value is null) continue;

                    await handler(result.Message.Value, cancellationToken).ConfigureAwait(false);
                }
                catch (ConsumeException ex)
                {
                    this.logger.LogError(ex, "Kafka consume exception on topic {Topic}", this.consumerOptions.Topic);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    this.logger.LogError(ex, "Unhandled exception processing trade message on topic {Topic}", this.consumerOptions.Topic);
                }
            }
        }
        catch (OperationCanceledException)
        {
            this.logger.LogInformation("Kafka trade consumer cancellation requested.");
        }
        finally
        {
            consumer.Close();
            this.logger.LogInformation("Kafka trade consumer closed.");
        }
    }

    private sealed class RawMarketTradeEventDeserializer : IDeserializer<RawMarketTradeEvent>
    {
        private readonly JsonSerializerOptions options;

        public RawMarketTradeEventDeserializer(JsonSerializerOptions options)
        {
            this.options = options;
        }

        public RawMarketTradeEvent Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (isNull || data.IsEmpty)
                throw new InvalidOperationException("Kafka message payload was empty.");

            return JsonSerializer.Deserialize<RawMarketTradeEvent>(data, this.options)
                ?? throw new JsonException("Unable to deserialize RawMarketTradeEvent from Kafka payload.");
        }
    }
}
