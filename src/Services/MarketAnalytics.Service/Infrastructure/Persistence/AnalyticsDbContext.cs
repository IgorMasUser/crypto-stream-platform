using Microsoft.EntityFrameworkCore;
using TradingApp.MarketAnalytics.Service.Domain;

namespace TradingApp.MarketAnalytics.Service.Infrastructure.Persistence;

public sealed class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    public DbSet<AggregatedMarketAnalyticsEntity> Aggregates => Set<AggregatedMarketAnalyticsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AggregatedMarketAnalyticsEntity>(e =>
        {
            e.ToTable("aggregated_market_analytics");

            // Composite PK: one row per symbol per window
            e.HasKey(x => new { x.Symbol, x.WindowStartUtc });

            e.Property(x => x.Symbol).HasMaxLength(20).IsRequired();
            e.Property(x => x.WindowStartUtc).IsRequired();
            e.Property(x => x.WindowEndUtc).IsRequired();
            e.Property(x => x.OpenPrice).HasColumnType("numeric(18,8)");
            e.Property(x => x.HighPrice).HasColumnType("numeric(18,8)");
            e.Property(x => x.LowPrice).HasColumnType("numeric(18,8)");
            e.Property(x => x.LastPrice).HasColumnType("numeric(18,8)");
            e.Property(x => x.Volume).HasColumnType("numeric(18,8)");
        });
    }
}
