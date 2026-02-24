using Microsoft.Extensions.Options;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;
using TradingApp.MarketAnalytics.Service.Application.Configuration;
using TradingApp.MarketAnalytics.Service.Infrastructure.Kafka;

namespace TradingApp.MarketAnalytics.Service.Worker.HostedServices;

public sealed class RawTradeConsumerHostedService : BackgroundService
{
    private readonly IRawTradeEventConsumer consumer;
    private readonly IRawTradeEventHandler handler;
    private readonly ILogger<RawTradeConsumerHostedService> logger;
    private readonly KafkaTradeConsumerOptions options;

    public RawTradeConsumerHostedService(
        IRawTradeEventConsumer consumer,
        IRawTradeEventHandler handler,
        IOptions<KafkaTradeConsumerOptions> options,
        ILogger<RawTradeConsumerHostedService> logger)
    {
        this.consumer = consumer;
        this.handler  = handler;
        this.logger   = logger;
        this.options  = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!this.options.Enabled)
        {
            this.logger.LogInformation("Raw trade consumer hosted service is disabled.");
            return;
        }

        this.logger.LogInformation("Raw trade consumer hosted service starting.");
        await this.consumer.ConsumeAsync(
            async (trade, token) =>
            {
                await this.handler.HandleAsync(trade, token).ConfigureAwait(false);
            },
            stoppingToken).ConfigureAwait(false);
    }
}
