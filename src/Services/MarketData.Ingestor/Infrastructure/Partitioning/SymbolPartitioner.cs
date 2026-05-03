namespace TradingApp.MarketData.Ingestor.Infrastructure.Partitioning;

/// <summary>
/// Distributes symbols across StatefulSet pod replicas using modulo partitioning.
///
/// Example with 3 pods and 15 symbols:
///   ingestor-0 (index 0): symbols at positions 0, 3,  6,  9, 12  → btcusdt, bnbusdt, dogeusdt, shibusdt, atomusdt
///   ingestor-1 (index 1): symbols at positions 1, 4,  7, 10, 13  → ethusdt, xrpusdt, maticusdt, ltcusdt,  trxusdt
///   ingestor-2 (index 2): symbols at positions 2, 5,  8, 11, 14  → solusdt, adausdt, dotusdt,  avaxusdt, linkusdt
/// </summary>
public static class SymbolPartitioner
{
    /// <summary>
    /// Returns the subset of symbols assigned to this pod.
    /// When totalPods == 1, all symbols are returned unchanged.
    /// </summary>
    public static string[] Partition(string[] allSymbols, int podIndex, int totalPods)
    {
        if (totalPods <= 1)
            return allSymbols;

        if (allSymbols.Length == 0)
            return allSymbols;

        return allSymbols
            .Where((_, i) => i % totalPods == podIndex)
            .ToArray();
    }

    /// <summary>
    /// Resolves pod index and total pod count from environment variables.
    ///   POD_NAME   — injected by Kubernetes Downward API (e.g. "ingestor-0")
    ///   TOTAL_PODS — set as a static env var in the StatefulSet spec
    /// Falls back to index=0 / total=1 when running locally.
    /// </summary>
    public static (int podIndex, int totalPods) ResolvePodIdentity(ILogger logger)
    {
        var podName   = Environment.GetEnvironmentVariable("POD_NAME")   ?? "ingestor-0";
        var totalPods = int.TryParse(Environment.GetEnvironmentVariable("TOTAL_PODS"), out var tp)
            ? Math.Max(1, tp)
            : 1;

        // StatefulSet pod names follow the pattern "<statefulset-name>-<ordinal>"
        var lastDash  = podName.LastIndexOf('-');
        var podIndex  = lastDash >= 0 && int.TryParse(podName[(lastDash + 1)..], out var idx)
            ? Math.Abs(idx) % totalPods
            : 0;

        logger.LogInformation(
            "Pod identity resolved: name={PodName} index={PodIndex}/{TotalPods}",
            podName, podIndex, totalPods);

        return (podIndex, totalPods);
    }
}
