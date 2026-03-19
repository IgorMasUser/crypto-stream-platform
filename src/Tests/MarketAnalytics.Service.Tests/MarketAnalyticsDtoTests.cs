using TradingApp.Dashboard.Web.Models;

namespace MarketAnalytics.Service.Tests;

public sealed class MarketAnalyticsDtoTests
{
    // ── ChangePercent ──────────────────────────────────────────────────────────

    [Fact]
    public void ChangePercent_PriceRose_ReturnsPositiveValue()
    {
        var dto = MakeDto(open: 100m, last: 110m);

        Assert.Equal(10.0, dto.ChangePercent, precision: 4);
    }

    [Fact]
    public void ChangePercent_PriceFell_ReturnsNegativeValue()
    {
        var dto = MakeDto(open: 100m, last: 90m);

        Assert.Equal(-10.0, dto.ChangePercent, precision: 4);
    }

    [Fact]
    public void ChangePercent_NoChange_ReturnsZero()
    {
        var dto = MakeDto(open: 100m, last: 100m);

        Assert.Equal(0.0, dto.ChangePercent, precision: 4);
    }

    [Fact]
    public void ChangePercent_ZeroOpenPrice_ReturnsZeroWithoutDivisionError()
    {
        var dto = MakeDto(open: 0m, last: 50m);

        Assert.Equal(0.0, dto.ChangePercent);
    }

    // ── RangePercent ──────────────────────────────────────────────────────────

    [Fact]
    public void RangePercent_NormalRange_ReturnsCorrectValue()
    {
        // high=110, low=90, open=100 → range = 20/100 = 20%
        var dto = MakeDto(open: 100m, high: 110m, low: 90m);

        Assert.Equal(20.0, dto.RangePercent, precision: 4);
    }

    [Fact]
    public void RangePercent_NoRange_ReturnsZero()
    {
        var dto = MakeDto(open: 100m, high: 100m, low: 100m);

        Assert.Equal(0.0, dto.RangePercent, precision: 4);
    }

    [Fact]
    public void RangePercent_ZeroOpenPrice_ReturnsZeroWithoutDivisionError()
    {
        var dto = MakeDto(open: 0m, high: 10m, low: 5m);

        Assert.Equal(0.0, dto.RangePercent);
    }

    // ── Theory: ChangePercent is symmetric ────────────────────────────────────

    [Theory]
    [InlineData(100,  105,  5.0)]
    [InlineData(100,   50, -50.0)]
    [InlineData(200,  300,  50.0)]
    [InlineData(1000, 999, -0.1)]
    public void ChangePercent_Various_ReturnsExpected(
        double open, double last, double expected)
    {
        var dto = MakeDto(open: (decimal)open, last: (decimal)last);

        Assert.Equal(expected, dto.ChangePercent, precision: 2);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static MarketAnalyticsDto MakeDto(
        decimal open  = 100m,
        decimal last  = 100m,
        decimal high  = 105m,
        decimal low   = 95m) =>
        new()
        {
            Symbol         = "BTCUSDT",
            WindowStartUtc = DateTime.UtcNow,
            WindowEndUtc   = DateTime.UtcNow.AddMinutes(1),
            OpenPrice      = open,
            HighPrice      = high,
            LowPrice       = low,
            LastPrice      = last,
            Volume         = 1m,
            TradesCount    = 1,
            CreatedAtUtc   = DateTime.UtcNow,
        };
}
