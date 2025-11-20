using System.Text.Json.Serialization;

namespace TradingApp.MarketData.Ingestor.Domain.Models;

public sealed record BinanceCombinedTradeMessage(
    [property: JsonPropertyName("stream")] string Stream,
    [property: JsonPropertyName("data")] BinanceTradeData Data);

public sealed class BinanceTradeData
{
    [JsonPropertyName("e")]
    public string EventType { get; init; } = default!;

    [JsonPropertyName("E")]
    public long EventTime { get; init; }

    [JsonPropertyName("s")]
    public string Symbol { get; init; } = default!;

    [JsonPropertyName("t")]
    public long TradeId { get; init; }

    [JsonPropertyName("p")]
    public string Price { get; init; } = default!;

    [JsonPropertyName("q")]
    public string Quantity { get; init; } = default!;

    [JsonPropertyName("b")]
    public long BuyerOrderId { get; init; }

    [JsonPropertyName("a")]
    public long SellerOrderId { get; init; }

    [JsonPropertyName("T")]
    public long TradeTime { get; init; }

    [JsonPropertyName("m")]
    public bool IsMarketMaker { get; init; }

    [JsonPropertyName("M")]
    public bool IsBestMatch { get; init; }
}

