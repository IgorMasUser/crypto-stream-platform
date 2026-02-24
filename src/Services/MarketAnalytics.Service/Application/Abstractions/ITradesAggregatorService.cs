using TradingApp.Contracts.Events;
using TradingApp.MarketAnalytics.Service.Domain;

namespace TradingApp.MarketAnalytics.Service.Application.Abstractions;

public interface ITradesAggregatorService
{
    Task<AggregatedMarketAnalyticsEntity> BuildAggregatedTradesAsync(
        DateTime windowStart,
        int aggregationRangeInMinutes,
        RawMarketTradeEvent trade,
        CancellationToken cancellationToken = default);
}
