namespace TradingApp.MarketData.Ingestor.Application.Configuration;

public sealed class KafkaResilienceOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 5;
    public int BaseDelayMilliseconds { get; set; } = 250;
    public int MaxDelayMilliseconds { get; set; } = 5_000;
    public int JitterMilliseconds { get; set; } = 250;
}

