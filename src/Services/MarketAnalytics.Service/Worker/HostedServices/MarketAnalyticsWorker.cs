
namespace TradingApp.MarketAnalytics.Service.Worker.HostedServices;

public sealed class MarketAnalyticsWorker : BackgroundService
{
    private readonly ILogger<MarketAnalyticsWorker> _logger;

    public MarketAnalyticsWorker(ILogger<MarketAnalyticsWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MarketAnalytics.Service worker running");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
        }
    }
}

