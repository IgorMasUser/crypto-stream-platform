namespace TradingApp.MarketData.Ingestor.Application.Configuration;

public sealed class MarketDataIngestorOptions
{
    public string KafkaTopic { get; set; } = "raw.market.trades";
    public string? RetryTopic { get; set; } = "raw.market.trades.retry";
    public string? DeadLetterTopic { get; set; } = "raw.market.trades.dlq";
    public int ThroughputLogIntervalSeconds { get; set; } = 30;
}

