using System.Collections.Concurrent;
using TradingApp.Contracts.Events;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;
using TradingApp.MarketAnalytics.Service.Domain;

namespace TradingApp.MarketAnalytics.Service.Application.Services
{
    public class TradesAggregatorService : ITradesAggregatorService
    {
        readonly ConcurrentDictionary<string, AggregatedMarketAnalyticsEntity> aggregator;
        private readonly ILogger<TradesAggregatorService> logger;

        AggregatedMarketAnalyticsEntity aggregatedMarketEntity;
        public TradesAggregatorService(ILogger<TradesAggregatorService> logger)
        {
            this.aggregator = new ConcurrentDictionary<string, AggregatedMarketAnalyticsEntity>();
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public AggregatedMarketAnalyticsEntity BuildAggregatedTrades(string aggregationKey,int aggragationRangeinMinutes, RawMarketTradeEvent trade)
        {
            try
            {
                if (!aggregator.TryGetValue(aggregationKey, out aggregatedMarketEntity!))
                {
                    aggregatedMarketEntity = AddFirstTrade(trade, aggregationKey, aggragationRangeinMinutes, new AggregatedMarketAnalyticsEntity());
                }
                else
                {
                    aggregatedMarketEntity = BuildUpTradesAggregation(trade, aggregationKey, aggregatedMarketEntity);
                }

                return aggregatedMarketEntity;
            }
            catch (Exception ex)
            {
                logger.LogError("Error occured in while building trade aggregate" + ex.Message);
                throw new Exception("Building up trades aggregat");
            }
        }

        private AggregatedMarketAnalyticsEntity BuildUpTradesAggregation(RawMarketTradeEvent trade, string aggregationKey, AggregatedMarketAnalyticsEntity aggregatedMarketEntity)
        {
            aggregatedMarketEntity.HighPrice = Math.Max(aggregatedMarketEntity.HighPrice, trade.Price);
            aggregatedMarketEntity.LowPrice = Math.Min(aggregatedMarketEntity.LowPrice, trade.Price);
            aggregatedMarketEntity.LastPrice = trade.Price;

            aggregatedMarketEntity.Volume += trade.Quantity;
            aggregatedMarketEntity.TradesCount += 1;

            aggregator.TryAdd<string, AggregatedMarketAnalyticsEntity>(aggregationKey, aggregatedMarketEntity);

            return aggregatedMarketEntity;
        }


        private AggregatedMarketAnalyticsEntity AddFirstTrade(RawMarketTradeEvent trade, string aggregationKey, int aggragationRangeinMinutes, AggregatedMarketAnalyticsEntity aggregatedMarketEntity)
        {
            if(aggregatedMarketEntity is null) throw new ArgumentNullException(nameof(aggregatedMarketEntity));
            if(trade is null) throw new ArgumentNullException(nameof(trade));

            aggregatedMarketEntity.WindowStartUtc = trade.EventTimeUtc;
            aggregatedMarketEntity.WindowEndUtc = trade.EventTimeUtc.AddMinutes(aggragationRangeinMinutes);

            aggregatedMarketEntity.OpenPrice = trade.Price;
            aggregatedMarketEntity.HighPrice = trade.Price;
            aggregatedMarketEntity.LowPrice = trade.Price;
            aggregatedMarketEntity.LastPrice = trade.Price;

            aggregatedMarketEntity.Volume += trade.Quantity;
            aggregatedMarketEntity.TradesCount += 1;

            aggregator.TryAdd<string, AggregatedMarketAnalyticsEntity>(aggregationKey, aggregatedMarketEntity);

            return aggregatedMarketEntity;
        }
    }
}
