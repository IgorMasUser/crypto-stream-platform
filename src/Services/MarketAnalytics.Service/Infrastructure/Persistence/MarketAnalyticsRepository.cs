using Microsoft.EntityFrameworkCore;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;
using TradingApp.MarketAnalytics.Service.Domain;

namespace TradingApp.MarketAnalytics.Service.Infrastructure.Persistence;

public sealed class MarketAnalyticsRepository : IMarketAnalyticsRepository
{
    private readonly IDbContextFactory<AnalyticsDbContext> dbFactory;

    public MarketAnalyticsRepository(IDbContextFactory<AnalyticsDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    public async Task<AggregatedMarketAnalyticsEntity?> GetByWindowAsync(
        string symbol,
        DateTime windowStartUtc,
        CancellationToken cancellationToken = default)
    {
        await using var context = await this.dbFactory.CreateDbContextAsync(cancellationToken);
        return await context.Aggregates
            .FindAsync([symbol, windowStartUtc], cancellationToken);
    }

    public async Task SaveAsync(
        AggregatedMarketAnalyticsEntity entity,
        CancellationToken cancellationToken = default)
    {
        await using var context = await this.dbFactory.CreateDbContextAsync(cancellationToken);

        var exists = await context.Aggregates
            .FindAsync([entity.Symbol, entity.WindowStartUtc], cancellationToken);

        if (exists is null)
            context.Aggregates.Add(entity);
        else
            context.Entry(exists).CurrentValues.SetValues(entity);

        await context.SaveChangesAsync(cancellationToken);
    }
}
