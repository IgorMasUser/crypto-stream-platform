using TradingApp.MarketData.Ingestor.Application.Abstractions;

namespace TradingApp.MarketData.Ingestor.Worker.HostedServices;

public sealed class MarketDataIngestionWorker : BackgroundService
{
    private readonly IBinanceTradeStream _tradeStream;
    private readonly ITradeIngestionPipeline _pipeline;
    private readonly ILogger<MarketDataIngestionWorker> _logger;

    public MarketDataIngestionWorker(
        IBinanceTradeStream tradeStream,
        ITradeIngestionPipeline pipeline,
        ILogger<MarketDataIngestionWorker> logger)
    {
        _tradeStream = tradeStream;
        _pipeline = pipeline;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MarketData.Ingestor worker starting");

        try
        {
            await foreach (var message in _tradeStream.SubscribeAsync(stoppingToken))
            {
                await _pipeline.HandleAsync(message, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MarketData.Ingestor worker stopping");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "MarketData.Ingestor worker encountered a fatal error");
            throw;
        }
    }
}

