namespace TradingApp.Kafka.Abstractions;

public interface IKafkaProducer<TKey, TValue>
{
    Task ProduceAsync(string topic, TKey key, TValue value, CancellationToken cancellationToken = default);
}

