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
        public AggregatedMarketAnalyticsEntity BuildAggregatedTrades(DateTime windowStart,int aggragationRangeinMinutes, RawMarketTradeEvent trade)
        {
            try
            {
                if (trade is null) throw new ArgumentNullException(nameof(trade));

                var windowEnd = windowStart.AddMinutes(aggragationRangeinMinutes);

                var aggregationKey = $"{trade.Symbol}|{windowStart:O}";

                aggregatedMarketEntity = aggregator.AddOrUpdate(
                    aggregationKey,
                    _ => new AggregatedMarketAnalyticsEntity
                    {
                        Symbol = trade.Symbol,
                        CreatedAtUtc = DateTime.UtcNow,
                        WindowStartUtc = windowStart,
                        WindowEndUtc = windowEnd,
                        OpenPrice = trade.Price,
                        HighPrice = trade.Price,
                        LowPrice = trade.Price,
                        LastPrice = trade.Price,
                        Volume = trade.Quantity,
                        TradesCount = 1
                    },
                    (_, existing) =>
                    {
                        lock (existing)
                        {
                            existing.WindowStartUtc = windowStart;
                            existing.WindowEndUtc = windowEnd;
                            if (string.IsNullOrWhiteSpace(existing.Symbol))
                                existing.Symbol = trade.Symbol;

                            existing.HighPrice = Math.Max(existing.HighPrice, trade.Price);
                            existing.LowPrice = Math.Min(existing.LowPrice, trade.Price);
                            existing.LastPrice = trade.Price;
                            existing.Volume += trade.Quantity;
                            existing.TradesCount += 1;

                            return existing;
                        }
                    });

                return aggregatedMarketEntity;
            }
            catch (Exception ex)
            {
                logger.LogError("Error occured in while building trade aggregate" + ex.Message);
                throw new Exception("Building up trades aggregat");
            }
        }

    }
}
