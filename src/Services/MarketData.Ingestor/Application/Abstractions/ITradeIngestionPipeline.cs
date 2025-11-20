using TradingApp.MarketData.Ingestor.Domain.Models;

namespace TradingApp.MarketData.Ingestor.Application.Abstractions;

public interface ITradeIngestionPipeline
{
    Task HandleAsync(BinanceCombinedTradeMessage message, CancellationToken cancellationToken);
}

