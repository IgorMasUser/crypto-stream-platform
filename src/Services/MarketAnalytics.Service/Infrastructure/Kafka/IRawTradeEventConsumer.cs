using TradingApp.Contracts.Events;

namespace TradingApp.MarketAnalytics.Service.Infrastructure.Kafka;

public interface IRawTradeEventConsumer
{
    Task ConsumeAsync(Func<RawMarketTradeEvent, CancellationToken, Task> handler, CancellationToken cancellationToken);
}


