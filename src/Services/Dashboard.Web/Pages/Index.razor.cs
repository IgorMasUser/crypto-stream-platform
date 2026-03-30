using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using TradingApp.Dashboard.Web.Models;
using TradingApp.Dashboard.Web.Services;

namespace TradingApp.Dashboard.Web.Pages;

public enum SortField
{
    WindowStartUtc,
    Symbol,
    LastPrice,
    Volume,
    TradesCount,
    ChangePercent,
    RangePercent
}

public partial class Index : IAsyncDisposable
{
    [Inject] private AnalyticsService AnalyticsService { get; set; } = default!;
    [Inject] private ILogger<Index> Logger { get; set; } = default!;

    private bool _loading = true;
    private bool _autoRefresh = false;
    private readonly int _autoRefreshSec = 10;
    private int _pageSize = 100;
    private string _selectedSymbol = "";
    private SortField _sortField = SortField.WindowStartUtc;
    private DateTime? _lastUpdated;
    private string? _error;
    private List<MarketAnalyticsDto> _items = new();
    private List<string> _symbols = new();
    private CancellationTokenSource? _autoRefreshCts;

    // ── Sorting ─────────────────────────────────────────────────────────────

    private IEnumerable<MarketAnalyticsDto> _sortedItems =>
        _sortField switch
        {
            SortField.Symbol        => _items.OrderBy(x => x.Symbol),
            SortField.LastPrice     => _items.OrderByDescending(x => x.LastPrice),
            SortField.Volume        => _items.OrderByDescending(x => x.Volume),
            SortField.TradesCount   => _items.OrderByDescending(x => x.TradesCount),
            SortField.ChangePercent => _items.OrderByDescending(x => x.ChangePercent),
            SortField.RangePercent  => _items.OrderByDescending(x => x.RangePercent),
            _                       => _items
        };

    // ── KPI ──────────────────────────────────────────────────────────────────

    private IEnumerable<MarketAnalyticsDto> _latestPerSymbol =>
        _items.GroupBy(x => x.Symbol).Select(g => g.First());

    private MarketAnalyticsDto? _kpiBiggestGainer =>
        _latestPerSymbol.MaxBy(x => x.ChangePercent);

    private MarketAnalyticsDto? _kpiBiggestLoser =>
        _latestPerSymbol.MinBy(x => x.ChangePercent);

    private MarketAnalyticsDto? _kpiMostActive =>
        _latestPerSymbol.MaxBy(x => x.TradesCount);

    private IEnumerable<MarketAnalyticsDto> _topGainers =>
        _latestPerSymbol.OrderByDescending(x => x.ChangePercent).Take(5);

    private IEnumerable<MarketAnalyticsDto> _topLosers =>
        _latestPerSymbol.OrderBy(x => x.ChangePercent).Take(5);

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _symbols = (await AnalyticsService.GetSymbolsAsync()).OrderBy(s => s).ToList();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialise dashboard data");
            _error = "Failed to load data. Please try refreshing the page.";
            _loading = false;
        }
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var symbol = string.IsNullOrWhiteSpace(_selectedSymbol) ? null : _selectedSymbol;
            _items = (await AnalyticsService.GetAsync(_pageSize, symbol)).ToList();
            _lastUpdated = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load analytics data");
            _error = "Failed to load data. Please try refreshing the page.";
        }
        finally
        {
            _loading = false;
        }
    }

    private Task ApplySortAsync()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task OnAutoRefreshChangedAsync()
    {
        await StopAutoRefreshAsync();
        if (_autoRefresh)
            StartAutoRefresh();
    }

    private void StartAutoRefresh()
    {
        _autoRefreshCts = new CancellationTokenSource();
        var token = _autoRefreshCts.Token;
        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_autoRefreshSec));
            try
            {
                while (!token.IsCancellationRequested && await timer.WaitForNextTickAsync(token))
                {
                    await InvokeAsync(async () =>
                    {
                        await LoadAsync();
                        StateHasChanged();
                    });
                }
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    private async Task StopAutoRefreshAsync()
    {
        if (_autoRefreshCts is not null)
        {
            await _autoRefreshCts.CancelAsync();
            _autoRefreshCts.Dispose();
            _autoRefreshCts = null;
        }
    }

    public async ValueTask DisposeAsync() => await StopAutoRefreshAsync();
}
