using Microsoft.Extensions.Logging;
using TradingApp.Kafka.Abstractions;

namespace TradingApp.Kafka.Producers;

internal sealed class NullKafkaProducer<TKey, TValue> : IKafkaProducer<TKey, TValue>
{
    private readonly ILogger<NullKafkaProducer<TKey, TValue>> _logger;

    public NullKafkaProducer(ILogger<NullKafkaProducer<TKey, TValue>> logger)
    {
        _logger = logger;
    }

    public Task ProduceAsync(string topic, TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Kafka disabled. Dropping message for topic {Topic}", topic);
        return Task.CompletedTask;
    }
}

