using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TradingApp.AnalyticsIndexer.Worker;

/// <summary>
/// Placeholder worker for consuming aggregated events and indexing them into Elastic.
/// TODO: wire Kafka consumer on topic "aggregated-market-analytics" and use TradingApp.Elastic client.
/// </summary>
public sealed class IndexerHostedService : BackgroundService
{
    private readonly ILogger<IndexerHostedService> _logger;

    public IndexerHostedService(ILogger<IndexerHostedService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AnalyticsIndexer.Worker started. TODO: consume Kafka topic 'aggregated-market-analytics' and index to Elastic.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}

