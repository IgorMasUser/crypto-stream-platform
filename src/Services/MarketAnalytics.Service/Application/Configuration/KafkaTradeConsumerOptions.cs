using Confluent.Kafka;

namespace TradingApp.MarketAnalytics.Service.Application.Configuration;

public sealed class KafkaTradeConsumerOptions
{
    public bool Enabled { get; set; } = true;
    public string Topic { get; set; } = "raw-market-trades";
    public string GroupId { get; set; } = "marketanalytics-service";
    public bool EnableAutoCommit { get; set; } = true;

    /// <summary>
    /// When false (recommended), offsets are only stored for commit after the handler
    /// confirms successful processing, preventing data loss on crash mid-handler.
    /// </summary>
    public bool EnableAutoOffsetStore { get; set; } = false;
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
    public int? StatisticsIntervalMs { get; set; } = 60000;
}


