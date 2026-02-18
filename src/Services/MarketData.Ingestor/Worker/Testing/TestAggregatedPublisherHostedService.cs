using Microsoft.Extensions.Logging;
using TradingApp.Contracts.Events;
using TradingApp.Kafka.Abstractions;

namespace TradingApp.MarketData.Ingestor.Worker.Testing;

/// <summary>
/// Publishes a single test aggregated event on startup to validate the pipeline end-to-end.
/// </summary>
public sealed class TestAggregatedPublisherHostedService : IHostedService
{
    private readonly IKafkaProducer<string, AggregatedMarketAnalyticsEvent> _producer;
    private readonly ILogger<TestAggregatedPublisherHostedService> _logger;

    public TestAggregatedPublisherHostedService(
        IKafkaProducer<string, AggregatedMarketAnalyticsEvent> producer,
        ILogger<TestAggregatedPublisherHostedService> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-1);
        var evt = new AggregatedMarketAnalyticsEvent(
            Symbol: "BTCUSDT",
            WindowStartUtc: windowStart,
            WindowEndUtc: DateTime.UtcNow,
            OpenPrice: 100m,
            HighPrice: 110m,
            LowPrice: 95m,
            LastPrice: 105m,
            Volume: 123.45m,
            TradesCount: 10,
            CreatedAtUtc: DateTime.UtcNow);

        var key = $"{evt.Symbol}|{evt.WindowStartUtc:O}";
        _logger.LogInformation("Publishing test aggregated event {Key} for {Symbol}", key, evt.Symbol);
        await _producer.ProduceAsync("aggregated-market-analytics", key, evt, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

