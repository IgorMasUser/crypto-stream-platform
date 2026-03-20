using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using TradingApp.Elastic.Configuration;
using TradingApp.Elastic.Extensions;
using TradingApp.AnalyticsIndexer.Worker;
using TradingApp.AnalyticsIndexer.Worker.Options;

var builder = Host.CreateApplicationBuilder(args);

var esUrl = builder.Configuration["Logging:Elasticsearch:Url"] ?? "http://elastic:9200";

builder.Services.AddSerilog((_, loggerConfig) =>
    loggerConfig
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("service", "analytics-indexer-worker")
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

builder.Services.Configure<ElasticOptions>(builder.Configuration.GetSection("Elastic"));
builder.Services.Configure<IndexerOptions>(builder.Configuration.GetSection("Indexer"));
builder.Services.AddElasticClient();
builder.Services.AddHostedService<IndexerHostedService>();

var app = builder.Build();

await app.RunAsync();
