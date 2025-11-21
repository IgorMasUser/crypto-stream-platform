namespace TradingApp.Contracts.Events;

public record RawMarketTradeEvent(
    string EventId,
    string Source,          // "binance"
    string Symbol,          // "BTCUSDT" also key for hashing: partition = hash(key) % partitionCount. Same key always in the same partition
    string Stream,          // "btcusdt@trade"
    decimal Price,          // p
    decimal Quantity,       // q
    long TradeId,           // t
    bool? IsBuyerMaker,     // m 
    DateTime TradeTimeUtc,  // T
    DateTime EventTimeUtc,  // E
    DateTime IngestedAtUtc  // DateTime.UtcNow in Ingestor
);


