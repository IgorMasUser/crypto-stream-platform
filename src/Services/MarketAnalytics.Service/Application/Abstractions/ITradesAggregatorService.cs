using TradingApp.Contracts.Events;
using TradingApp.MarketAnalytics.Service.Domain;

namespace TradingApp.MarketAnalytics.Service.Application.Abstractions
{
    public interface ITradesAggregatorService
    {
        AggregatedMarketAnalyticsEntity BuildAggregatedTrades(DateTime windowStart, int aggregationRangeInMinutes, RawMarketTradeEvent trade);
    }
}
