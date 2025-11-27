using TradingApp.Contracts.Events;
using TradingApp.Kafka.Abstractions;

namespace TradingApp.MarketData.Ingestor.Worker.Testing;

/// <summary>
/// Simple development-only publisher that emits a sample trade to Kafka when the worker boots.
/// </summary>
public sealed class TestTradePublisherHostedService : IHostedService
{
    private readonly IKafkaProducer<string, RawMarketTradeEvent> _producer;
    private readonly ILogger<TestTradePublisherHostedService> _logger;

    public TestTradePublisherHostedService(
        IKafkaProducer<string, RawMarketTradeEvent> producer,
        ILogger<TestTradePublisherHostedService> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var sample = new RawMarketTradeEvent(
                Guid.NewGuid().ToString("N"),
                "binance",
                "BTCUSDT",
                "btcusdt@trade",
                42000.00m,
                0.25m,
                Random.Shared.NextInt64(1_000_000_000),
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                DateTime.UtcNow);

            _logger.LogInformation("Publishing sample trade event {EventId} to Kafka for validation.", sample.EventId);
            await _producer.ProduceAsync("raw-market-trades", sample.Symbol, sample, cancellationToken);
            _logger.LogInformation("Sample trade event {EventId} published successfully.", sample.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish sample trade event.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

