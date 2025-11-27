using TradingApp.Contracts.Events;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;

namespace TradingApp.MarketAnalytics.Service.Application.Handlers;

public sealed class LoggingRawTradeEventHandler : IRawTradeEventHandler
{
    private readonly ILogger<LoggingRawTradeEventHandler> _logger;

    public LoggingRawTradeEventHandler(ILogger<LoggingRawTradeEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(RawMarketTradeEvent trade, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processed trade {TradeId} ({Symbol}) at {Price} qty {Quantity} (EventId={EventId})",
            trade.TradeId,
            trade.Symbol,
            trade.Price,
            trade.Quantity,
            trade.EventId);

        return Task.CompletedTask;
    }
}


