using TradingApp.Contracts.Events;
using TradingApp.Kafka.Configuration;
using TradingApp.Kafka.Extensions;
using TradingApp.MarketData.Ingestor.Application.Abstractions;
using TradingApp.MarketData.Ingestor.Application.Configuration;
using TradingApp.MarketData.Ingestor.Application.Services;
using TradingApp.MarketData.Ingestor.Infrastructure.Binance;
using TradingApp.MarketData.Ingestor.Worker.HostedServices;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaProducerOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<BinanceStreamOptions>(builder.Configuration.GetSection("Binance"));
builder.Services.Configure<MarketDataIngestorOptions>(builder.Configuration.GetSection("Ingestor"));
builder.Services.Configure<KafkaResilienceOptions>(builder.Configuration.GetSection("Kafka:Resilience"));

var kafkaOptions = builder.Configuration.GetSection("Kafka").Get<KafkaProducerOptions>() ?? new KafkaProducerOptions();

if (kafkaOptions.Enabled && !string.IsNullOrWhiteSpace(kafkaOptions.BootstrapServers))
{
    builder.Services.AddKafkaProducer<string, RawMarketTradeEvent>();
}
else
{
    builder.Services.AddNullKafkaProducer<string, RawMarketTradeEvent>();
}
builder.Services.AddSingleton<IBinanceTradeStream, BinanceTradeStream>();
builder.Services.AddSingleton<ITradeIngestionPipeline, TradeIngestionPipeline>();
builder.Services.AddHostedService<MarketDataIngestionWorker>();

var app = builder.Build();
await app.RunAsync();

