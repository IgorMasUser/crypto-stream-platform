using Microsoft.Extensions.Options;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;
using TradingApp.MarketAnalytics.Service.Application.Configuration;
using TradingApp.MarketAnalytics.Service.Infrastructure.Kafka;

namespace TradingApp.MarketAnalytics.Service.Worker.HostedServices;

public sealed class RawTradeConsumerHostedService : BackgroundService
{
    private readonly IRawTradeEventConsumer _consumer;
    private readonly IRawTradeEventHandler _handler;
    private readonly ILogger<RawTradeConsumerHostedService> _logger;
    private readonly KafkaTradeConsumerOptions _options;

    public RawTradeConsumerHostedService(
        IRawTradeEventConsumer consumer,
        IRawTradeEventHandler handler,
        IOptions<KafkaTradeConsumerOptions> options,
        ILogger<RawTradeConsumerHostedService> logger)
    {
        _consumer = consumer;
        _handler = handler;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Raw trade consumer hosted service is disabled.");
            return;
        }

        _logger.LogInformation("Raw trade consumer hosted service starting.");
        await _consumer.ConsumeAsync(
            async (trade, token) =>
            {
                await _handler.HandleAsync(trade, token).ConfigureAwait(false);
            },
            stoppingToken).ConfigureAwait(false);
    }
}


