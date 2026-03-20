using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using TradingApp.Kafka.Configuration;
using TradingApp.Kafka.Extensions;
using TradingApp.MarketAnalytics.Service.Worker.HostedServices;
using TradingApp.MarketAnalytics.Service.Application.Configuration;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;
using TradingApp.MarketAnalytics.Service.Application.Services;
using TradingApp.MarketAnalytics.Service.Infrastructure.Kafka;
using TradingApp.MarketAnalytics.Service.Infrastructure.Persistence;
using TradingApp.Contracts.Events;

var builder = Host.CreateApplicationBuilder(args);

var esUrl = builder.Configuration["Logging:Elasticsearch:Url"] ?? "http://elastic:9200";

builder.Services.AddSerilog((_, loggerConfig) =>
    loggerConfig
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("service", "marketanalytics-service")
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

// Kafka
builder.Services.Configure<KafkaProducerOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddKafkaProducer<string, AggregatedMarketAnalyticsEvent>();
builder.Services.Configure<KafkaTradeConsumerOptions>(builder.Configuration.GetSection("Kafka:Consumer"));

// PostgreSQL via EF Core — IDbContextFactory allows use inside Singleton services
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

builder.Services.AddDbContextFactory<AnalyticsDbContext>(opt =>
    opt.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

// Repository (infrastructure) + Application services
builder.Services.AddSingleton<IMarketAnalyticsRepository, MarketAnalyticsRepository>();
builder.Services.AddSingleton<ITradesAggregatorService, TradesAggregatorService>();
builder.Services.AddSingleton<IRawTradeEventHandler, AggregatingRawTradeEventHandler>();
builder.Services.AddSingleton<IRawTradeEventConsumer, KafkaRawTradeEventConsumer>();
builder.Services.AddHostedService<RawTradeConsumerHostedService>();

var app = builder.Build();

// Apply pending EF Core migrations on startup (idempotent, safe to re-run)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
    await db.Database.MigrateAsync();
}

await app.RunAsync();
