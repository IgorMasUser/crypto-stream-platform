using System.Text.Json.Serialization;

namespace TradingApp.MarketData.Ingestor.Domain.Models;

public sealed record BinanceCombinedTradeMessage(
    [property: JsonPropertyName("stream")] string Stream,
    [property: JsonPropertyName("data")] BinanceTradeData Data);

public sealed class BinanceTradeData
{
    /// <summary>
    /// Event type, e.g. "trade".
    /// </summary>
    [JsonPropertyName("e")]
    public string EventType { get; init; } = default!;

    /// <summary>
    /// Event time in milliseconds since Unix epoch.
    /// </summary>
    [JsonPropertyName("E")]
    public long EventTime { get; init; }

    /// <summary>
    /// Trading symbol, e.g. "BTCUSDT".
    /// </summary>
    [JsonPropertyName("s")]
    public string Symbol { get; init; } = default!;

    /// <summary>
    /// Trade identifier (Binance trade ID).
    /// </summary>
    [JsonPropertyName("t")]
    public long TradeId { get; init; }

    /// <summary>
    /// Trade price as a string (decimal).
    /// </summary>
    [JsonPropertyName("p")]
    public string Price { get; init; } = default!;

    /// <summary>
    /// Executed quantity as a string (decimal).
    /// </summary>
    [JsonPropertyName("q")]
    public string Quantity { get; init; } = default!;

    /// <summary>
    /// Trade execution time in milliseconds since Unix epoch.
    /// </summary>
    [JsonPropertyName("T")]
    public long TradeTime { get; init; }

    /// <summary>
    /// True if the buyer is the market maker.
    /// </summary>
    [JsonPropertyName("m")]
    public bool IsMarketMaker { get; init; }

    /// <summary>
    /// True if the trade was the best price match.
    /// </summary>
    [JsonPropertyName("M")]
    public bool IsBestMatch { get; init; }
}

