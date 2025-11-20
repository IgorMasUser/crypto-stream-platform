using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TradingApp.Elastic.Clients;

namespace TradingApp.Elastic.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElasticClient(this IServiceCollection services)
    {
        services.TryAddSingleton<ElasticClientFactory>();
        services.TryAddSingleton<ElasticsearchClient>(sp => sp.GetRequiredService<ElasticClientFactory>().CreateClient());
        return services;
    }
}

