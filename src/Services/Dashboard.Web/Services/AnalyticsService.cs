using Elastic.Clients.Elasticsearch;
using TradingApp.Contracts.Events;

namespace TradingApp.Dashboard.Web.Services;

public sealed class AnalyticsService
{
    private readonly ElasticsearchClient _client;
    private readonly IConfiguration _config;

    public AnalyticsService(ElasticsearchClient client, IConfiguration config)
    {
        _client = client;
        _config = config;
    }

    public async Task<IReadOnlyCollection<AggregatedMarketAnalyticsEvent>> GetAsync(int size = 50, CancellationToken ct = default)
    {
        var index = _config.GetValue<string>("Elastic:IndexPrefix") ?? "analytics-index";
        var resp = await _client.SearchAsync<AggregatedMarketAnalyticsEvent>(s => s
            .Index(index)
            .Size(size), ct);

        return resp.Documents;
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        var index = _config.GetValue<string>("Elastic:IndexPrefix") ?? "analytics-index";
        await _client.DeleteByQueryAsync<AggregatedMarketAnalyticsEvent>(index, q => q
            .Query(_ => _.MatchAll()), ct);
    }
}

