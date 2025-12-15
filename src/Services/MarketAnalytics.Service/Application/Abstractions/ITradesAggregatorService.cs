using TradingApp.Contracts.Events;
using TradingApp.MarketAnalytics.Service.Domain;

namespace TradingApp.MarketAnalytics.Service.Application.Abstractions
{
    public interface ITradesAggregatorService
    {
        AggregatedMarketAnalyticsEntity BuildAggregatedTrades(string aggregationKey, int aggragationRangeinMinutes, RawMarketTradeEvent trade);
    }
}
