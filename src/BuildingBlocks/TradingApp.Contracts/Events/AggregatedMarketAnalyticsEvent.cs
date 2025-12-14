namespace TradingApp.Contracts.Events;

public record AggregatedMarketAnalyticsEvent(
    string Symbol,              // Symbol the window belongs to (e.g., BTCUSDT)
    DateTime WindowStartUtc,    // Window start (UTC)
    DateTime WindowEndUtc,      // Window end (UTC)
    decimal OpenPrice,          // OHLC: open
    decimal HighPrice,          // OHLC: high
    decimal LowPrice,           // OHLC: low
    decimal LastPrice,          // OHLC: close (last price in window)
    decimal Volume,             // Total volume in window
    int TradesCount,            // Number of trades in window
    DateTime CreatedAtUtc       // When aggregate was produced (UTC)
);
