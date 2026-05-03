namespace TradingApp.MarketData.Ingestor.Application.Configuration;

public sealed class MarketDataIngestorOptions
{
    public string KafkaTopic { get; set; } = "raw-market-trades";
    public string? RetryTopic { get; set; } = "raw-market-trades-retry";
    public string? DeadLetterTopic { get; set; } = "raw-market-trades-dlq";
    public int ThroughputLogIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Configuration for the background consumer that re-processes
    /// messages from the retry topic.
    /// </summary>
    public RetryConsumerOptions RetryConsumer { get; set; } = new();
}

public sealed class RetryConsumerOptions
{
    /// <summary>Kafka consumer group for the retry consumer.</summary>
    public string GroupId { get; set; } = "marketdata-ingestor-retry";

    /// <summary>
    /// Seconds to wait before re-publishing a message from the retry topic.
    /// Gives the broker time to recover after a transient failure.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 30;
}

