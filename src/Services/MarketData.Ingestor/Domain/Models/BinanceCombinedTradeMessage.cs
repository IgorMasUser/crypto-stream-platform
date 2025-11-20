using System.Text.Json.Serialization;

namespace TradingApp.MarketData.Ingestor.Domain.Models;

public sealed record BinanceCombinedTradeMessage(
    [property: JsonPropertyName("stream")] string Stream,
    [property: JsonPropertyName("data")] BinanceTradeData Data);

public sealed record BinanceTradeData(
    [property: JsonPropertyName("e")] string EventType,
    [property: JsonPropertyName("E")] long EventTime,
    [property: JsonPropertyName("s")] string Symbol,
    [property: JsonPropertyName("t")] long TradeId,
    [property: JsonPropertyName("p")] string Price,
    [property: JsonPropertyName("q")] string Quantity,
    [property: JsonPropertyName("b")] long BuyerOrderId,
    [property: JsonPropertyName("a")] long SellerOrderId,
    [property: JsonPropertyName("T")] long TradeTime,
    [property: JsonPropertyName("m")] bool IsMarketMaker);

