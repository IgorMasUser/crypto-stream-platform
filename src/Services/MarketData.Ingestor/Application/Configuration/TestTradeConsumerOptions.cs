using Confluent.Kafka;

namespace TradingApp.MarketData.Ingestor.Application.Configuration;

public sealed class TestTradeConsumerOptions
{
    public bool Enabled { get; set; }
    public string Topic { get; set; } = "raw-market-trades";
    public string GroupId { get; set; } = "marketdata-ingestor-test-consumer";
    public bool EnableAutoCommit { get; set; } = true;
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
}


