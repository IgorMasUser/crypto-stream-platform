using TradingApp.MarketAnalytics.Service.Domain;

namespace TradingApp.MarketAnalytics.Service.Application.Abstractions;

public interface IMarketAnalyticsRepository
{
    /// <summary>
    /// Atomically reads the aggregate for the given symbol and window, then either:
    /// <list type="bullet">
    ///   <item>calls <paramref name="createNew"/> when no row exists yet (first trade in the window), or</item>
    ///   <item>calls <paramref name="applyUpdate"/> to mutate the existing entity (subsequent trades).</item>
    /// </list>
    /// Persists the result and returns the saved entity.
    /// A unique-constraint conflict caused by a concurrent replica is handled transparently
    /// by reloading the row and applying <paramref name="applyUpdate"/>.
    /// </summary>
    Task<AggregatedMarketAnalyticsEntity> UpsertWindowAsync(
        string symbol,
        DateTime windowStart,
        DateTime windowEnd,
        Func<AggregatedMarketAnalyticsEntity> createNew,
        Action<AggregatedMarketAnalyticsEntity> applyUpdate,
        CancellationToken cancellationToken = default);
}
