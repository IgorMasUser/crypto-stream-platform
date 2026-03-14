namespace TradingApp.Dashboard.Web.Models;

public sealed class MarketAnalyticsDto
{
    public required string Symbol { get; init; }
    public required DateTime WindowStartUtc { get; init; }
    public required DateTime WindowEndUtc { get; init; }
    public required decimal OpenPrice { get; init; }
    public required decimal HighPrice { get; init; }
    public required decimal LowPrice { get; init; }
    public required decimal LastPrice { get; init; }
    public required decimal Volume { get; init; }
    public required int TradesCount { get; init; }
    public required DateTime CreatedAtUtc { get; init; }

    public double ChangePercent =>
        OpenPrice == 0 ? 0d : (double)((LastPrice - OpenPrice) / OpenPrice * 100m);

    public double RangePercent =>
        OpenPrice == 0 ? 0d : (double)((HighPrice - LowPrice) / OpenPrice * 100m);
}
