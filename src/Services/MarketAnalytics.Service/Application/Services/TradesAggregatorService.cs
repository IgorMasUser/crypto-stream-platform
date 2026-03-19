using Microsoft.Extensions.Logging;
using TradingApp.Contracts.Events;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;
using TradingApp.MarketAnalytics.Service.Domain;

namespace TradingApp.MarketAnalytics.Service.Application.Services;

public sealed class TradesAggregatorService : ITradesAggregatorService
{
    private readonly IMarketAnalyticsRepository repository;
    private readonly ILogger<TradesAggregatorService> logger;

    public TradesAggregatorService(
        IMarketAnalyticsRepository repository,
        ILogger<TradesAggregatorService> logger)
    {
        this.repository = repository;
        this.logger     = logger;
    }

    public async Task<AggregatedMarketAnalyticsEntity> BuildAggregatedTradesAsync(
        DateTime windowStart,
        int aggregationRangeInMinutes,
        RawMarketTradeEvent trade,
        CancellationToken cancellationToken = default)
    {
        if (trade is null) throw new ArgumentNullException(nameof(trade));

        var windowEnd = windowStart.AddMinutes(aggregationRangeInMinutes);

        var existing = await this.repository.GetByWindowAsync(trade.Symbol, windowStart, cancellationToken);

        AggregatedMarketAnalyticsEntity aggregate;

        if (existing is null)
        {
            aggregate = CreateNewWindow(trade, windowStart, windowEnd);
            this.logger.LogInformation(
                "New window opened: {Symbol} window={WindowStart:u} open={Open}",
                trade.Symbol, windowStart, trade.Price);
        }
        else
        {
            aggregate = UpdateWindow(existing, trade, windowEnd);
            this.logger.LogDebug(
                "Window updated: {Symbol} window={WindowStart:u} trades={Count} last={Last}",
                trade.Symbol, windowStart, aggregate.TradesCount, aggregate.LastPrice);
        }

        await this.repository.SaveAsync(aggregate, cancellationToken);

        return aggregate;
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private static AggregatedMarketAnalyticsEntity CreateNewWindow(
        RawMarketTradeEvent trade,
        DateTime windowStart,
        DateTime windowEnd) => new()
    {
        Symbol         = trade.Symbol,
        WindowStartUtc = windowStart,
        WindowEndUtc   = windowEnd,
        OpenPrice      = trade.Price,
        HighPrice      = trade.Price,
        LowPrice       = trade.Price,
        LastPrice      = trade.Price,
        Volume         = trade.Quantity,
        TradesCount    = 1,
        CreatedAtUtc   = DateTime.UtcNow,
        UpdatedAtUtc   = DateTime.UtcNow,
    };

    private static AggregatedMarketAnalyticsEntity UpdateWindow(
        AggregatedMarketAnalyticsEntity existing,
        RawMarketTradeEvent trade,
        DateTime windowEnd)
    {
        existing.HighPrice     = Math.Max(existing.HighPrice, trade.Price);
        existing.LowPrice      = Math.Min(existing.LowPrice, trade.Price);
        existing.LastPrice     = trade.Price;
        existing.Volume       += trade.Quantity;
        existing.TradesCount  += 1;
        existing.WindowEndUtc  = windowEnd;
        existing.UpdatedAtUtc  = DateTime.UtcNow;
        return existing;
    }
}
