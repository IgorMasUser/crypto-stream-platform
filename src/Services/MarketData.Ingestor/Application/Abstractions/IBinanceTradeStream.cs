using TradingApp.MarketData.Ingestor.Domain.Models;

namespace TradingApp.MarketData.Ingestor.Application.Abstractions;

public interface IBinanceTradeStream
{
    IAsyncEnumerable<BinanceCombinedTradeMessage> SubscribeAsync(CancellationToken cancellationToken);
}

