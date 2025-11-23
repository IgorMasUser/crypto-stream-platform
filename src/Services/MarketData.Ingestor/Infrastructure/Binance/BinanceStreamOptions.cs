namespace TradingApp.MarketData.Ingestor.Infrastructure.Binance;

public sealed class BinanceStreamOptions
{
    public string Endpoint { get; set; } = "wss://stream.binance.com:9443/stream";
    public string[] Symbols { get; set; } = Array.Empty<string>();
    public int ReceiveBufferSize { get; set; } = 32 * 1024;
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int MaxReconnectDelaySeconds { get; set; } = 60;
    public int ReconnectJitterSeconds { get; set; } = 2;
}

