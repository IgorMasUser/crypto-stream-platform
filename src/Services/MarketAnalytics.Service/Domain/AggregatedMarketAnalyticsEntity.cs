namespace TradingApp.MarketAnalytics.Service.Domain;

/// <summary>
/// OHLCV aggregation window persisted to PostgreSQL.
/// Composite PK: (Symbol, WindowStartUtc) — one row per symbol per minute.
/// </summary>
public class AggregatedMarketAnalyticsEntity
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime WindowStartUtc { get; set; }
    public DateTime WindowEndUtc { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal LastPrice { get; set; }
    public decimal Volume { get; set; }
    public int TradesCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
