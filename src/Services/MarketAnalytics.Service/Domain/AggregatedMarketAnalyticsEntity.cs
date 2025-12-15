using TradingApp.Contracts.Events;

namespace TradingApp.MarketAnalytics.Service.Domain;

/// <summary>
/// In-memory aggregation model for a 1-minute OHLCV window.
/// Holds state while events for the same window are processed.
/// </summary>
public class AggregatedMarketAnalyticsEntity
{
    public string Symbol { get; set; }
    public DateTime WindowStartUtc { get; set; }
    public DateTime WindowEndUtc { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal LastPrice { get; set; }
    public decimal Volume { get; set; }
    public int TradesCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
