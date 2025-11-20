using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TradingApp.Kafka.Abstractions;
using TradingApp.Kafka.Producers;

namespace TradingApp.Kafka.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaProducer<TKey, TValue>(this IServiceCollection services)
    {
        services.TryAddSingleton<IKafkaProducer<TKey, TValue>, KafkaProducer<TKey, TValue>>();
        return services;
    }
}

