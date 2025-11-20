namespace TradingApp.Contracts.Events;

public record RawMarketTradeEvent(
    string EventId,
    string Source,
    string Symbol,
    string Stream,
    decimal Price,
    decimal Quantity,
    long TradeId,
    DateTime EventTimeUtc,
    DateTime IngestedAtUtc);

