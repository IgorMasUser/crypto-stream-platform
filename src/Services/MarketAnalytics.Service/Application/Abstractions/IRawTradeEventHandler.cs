using TradingApp.Contracts.Events;

namespace TradingApp.MarketAnalytics.Service.Application.Abstractions;

public interface IRawTradeEventHandler
{
    Task HandleAsync(RawMarketTradeEvent trade, CancellationToken cancellationToken);
}


