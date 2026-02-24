using TradingApp.MarketAnalytics.Service.Domain;

namespace TradingApp.MarketAnalytics.Service.Application.Abstractions;

public interface IMarketAnalyticsRepository
{
    /// <summary>Returns the aggregate for the given symbol and window, or null if not found.</summary>
    Task<AggregatedMarketAnalyticsEntity?> GetByWindowAsync(
        string symbol,
        DateTime windowStartUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates the aggregate (upsert by composite key).</summary>
    Task SaveAsync(AggregatedMarketAnalyticsEntity entity, CancellationToken cancellationToken = default);
}
