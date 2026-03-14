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

    public async Task<IReadOnlyCollection<AggregatedMarketAnalyticsEvent>> GetAsync(
        int size = 100,
        string? symbol = null,
        CancellationToken ct = default)
    {
        var index = _config.GetValue<string>("Elastic:IndexPrefix") ?? "analytics-index";

        var resp = await _client.SearchAsync<AggregatedMarketAnalyticsEvent>(s =>
        {
            s.Index(index).Size(size);
            s.Sort(sort => sort.Field(f => f.WindowStartUtc, o => o.Order(SortOrder.Desc)));

            if (!string.IsNullOrWhiteSpace(symbol))
            {
                s.Query(q => q.Term(t => t.Field("symbol.keyword").Value(symbol)));
            }
        }, ct);

        return resp.Documents;
    }

    // Get distinct symbols by sampling recent documents
    public async Task<IReadOnlyCollection<string>> GetSymbolsAsync(CancellationToken ct = default)
    {
        var resp = await GetAsync(size: 500, symbol: null, ct);
        return resp.Select(x => x.Symbol)
                   .Where(s => !string.IsNullOrWhiteSpace(s))
                   .Distinct()
                   .OrderBy(s => s)
                   .ToList();
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        var index = _config.GetValue<string>("Elastic:IndexPrefix") ?? "analytics-index";
        await _client.DeleteByQueryAsync<AggregatedMarketAnalyticsEvent>(index, q => q
            .Query(_ => _.MatchAll()), ct);
    }
}

