namespace TradingApp.MarketData.Ingestor.Application.Configuration;

public sealed class MarketDataIngestorOptions
{
    public string KafkaTopic { get; set; } = "raw.market.trades";
}

