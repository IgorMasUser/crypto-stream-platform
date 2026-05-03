using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
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

var builder = Host.CreateApplicationBuilder(args);

var esUrl = builder.Configuration["Logging:Elasticsearch:Url"] ?? "http://elastic:9200";

builder.Services.AddSerilog((_, loggerConfig) =>
    loggerConfig
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "TradingApp.MarketData.Ingestor")
        .Enrich.WithProperty("Pod", Environment.GetEnvironmentVariable("POD_NAME") ?? Environment.MachineName)
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(esUrl))
        {
            AutoRegisterTemplate = true,
            IndexFormat          = "tradingapp-logs-{0:yyyy.MM}",
            TypeName             = null,
            FailureCallback      = (e, ex) => Console.Error.WriteLine($"[Serilog ES] Failed: {ex?.Message}")
        })
);

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
