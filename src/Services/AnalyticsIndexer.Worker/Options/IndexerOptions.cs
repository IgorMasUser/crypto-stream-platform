namespace TradingApp.AnalyticsIndexer.Worker.Options;

public sealed class IndexerOptions
{
    public string Topic { get; set; } = "aggregated-market-analytics";
    public string GroupId { get; set; } = "analytics-indexer-worker";
    public string IndexPrefix { get; set; } = "analytics-index";
    public string BootstrapServers { get; set; } = "kafka:9092";
}

