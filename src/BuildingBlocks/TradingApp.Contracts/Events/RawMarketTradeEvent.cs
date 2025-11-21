namespace TradingApp.Contracts.Events;

public record RawMarketTradeEvent(
    string EventId,
    string Source,          // "binance"
    string Symbol,          // "BTCUSDT"
    string Stream,          // "btcusdt@trade"
    decimal Price,          // p
    decimal Quantity,       // q
    long TradeId,           // t
    bool? IsBuyerMaker,     // m 
    long? BuyerOrderId,     // b 
    long? SellerOrderId,    // a 
    DateTime TradeTimeUtc,  // T
    DateTime EventTimeUtc,  // E
    DateTime IngestedAtUtc  // DateTime.UtcNow in Ingestor
);


