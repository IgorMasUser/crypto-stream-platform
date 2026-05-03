using TradingApp.Contracts.Events;
using TradingApp.Kafka.Configuration;
using TradingApp.Kafka.Extensions;
using TradingApp.MarketData.Ingestor.Application.Abstractions;
using TradingApp.MarketData.Ingestor.Application.Configuration;
using TradingApp.MarketData.Ingestor.Application.Services;
using TradingApp.MarketData.Ingestor.Infrastructure.Binance;
using TradingApp.MarketData.Ingestor.Infrastructure.Partitioning;
using TradingApp.MarketData.Ingestor.Worker.HostedServices;
using TradingApp.MarketData.Ingestor.Worker.Testing;
using TradingApp.Kafka.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaProducerOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<BinanceStreamOptions>(builder.Configuration.GetSection("Binance"));
builder.Services.Configure<MarketDataIngestorOptions>(builder.Configuration.GetSection("Ingestor"));
builder.Services.Configure<KafkaResilienceOptions>(builder.Configuration.GetSection("Kafka:Resilience"));

// Apply symbol partitioning based on StatefulSet pod identity.
builder.Services.Configure<BinanceStreamOptions>(options =>
{
    using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
    var logger = loggerFactory.CreateLogger("SymbolPartitioner");

    var (podIndex, totalPods) = SymbolPartitioner.ResolvePodIdentity(logger);
    var assigned = SymbolPartitioner.Partition(options.Symbols, podIndex, totalPods);

    logger.LogInformation(
        "Symbol partition: pod {PodIndex}/{TotalPods} → [{Symbols}]",
        podIndex, totalPods, string.Join(", ", assigned));

    options.Symbols = assigned;
});

var kafkaOptions = builder.Configuration.GetSection("Kafka").Get<KafkaProducerOptions>() ?? new KafkaProducerOptions();

if (kafkaOptions.Enabled && !string.IsNullOrWhiteSpace(kafkaOptions.BootstrapServers))
{
    builder.Services.AddKafkaProducer<string, RawMarketTradeEvent>();
    builder.Services.AddKafkaProducer<string, AggregatedMarketAnalyticsEvent>();
}
else
{
    builder.Services.AddNullKafkaProducer<string, RawMarketTradeEvent>();
    builder.Services.AddNullKafkaProducer<string, AggregatedMarketAnalyticsEvent>();
}

builder.Services.AddSingleton<IBinanceTradeStream, BinanceTradeStream>();
builder.Services.AddSingleton<ITradeIngestionPipeline, TradeIngestionPipeline>();
builder.Services.AddHostedService<MarketDataIngestionWorker>();
builder.Services.AddHostedService<RetryConsumerHostedService>();
builder.Services.AddHostedService<TestAggregatedPublisherHostedService>();

var app = builder.Build();

await app.RunAsync();
