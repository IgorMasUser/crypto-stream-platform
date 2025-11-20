using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingApp.Kafka.Abstractions;
using TradingApp.Kafka.Configuration;
using TradingApp.Kafka.Serialization;

namespace TradingApp.Kafka.Producers;

public sealed class KafkaProducer<TKey, TValue> : IKafkaProducer<TKey, TValue>, IDisposable
{
    private readonly IProducer<TKey, TValue> _producer;
    private readonly ILogger<KafkaProducer<TKey, TValue>> _logger;

    public KafkaProducer(IOptions<KafkaProducerOptions> options, ILogger<KafkaProducer<TKey, TValue>> logger)
    {
        _logger = logger;
        var config = options.Value.BuildProducerConfig();

        _producer = new ProducerBuilder<TKey, TValue>(config)
            .SetKeySerializer(CreateSerializer<TKey>())
            .SetValueSerializer(CreateSerializer<TValue>())
            .Build();
    }

    public async Task ProduceAsync(string topic, TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic must be provided.", nameof(topic));
        }

        try
        {
            var deliveryResult = await _producer.ProduceAsync(
                topic,
                new Message<TKey, TValue>
                {
                    Key = key,
                    Value = value
                },
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Delivered message to {Topic} partition {Partition} offset {Offset}",
                deliveryResult.Topic,
                deliveryResult.Partition,
                deliveryResult.Offset);
        }
        catch (ProduceException<TKey, TValue> ex)
        {
            _logger.LogError(ex, "Failed to deliver message to topic {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _producer.Flush(TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while flushing Kafka producer");
        }
        finally
        {
            _producer.Dispose();
        }
    }

    private static ISerializer<T> CreateSerializer<T>()
    {
        if (typeof(T) == typeof(string))
        {
            return (ISerializer<T>)Serializers.Utf8;
        }

        if (typeof(T) == typeof(byte[]))
        {
            return (ISerializer<T>)Serializers.ByteArray;
        }

        return new SystemTextJsonSerializer<T>();
    }
}

