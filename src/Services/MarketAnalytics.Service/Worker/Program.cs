using Microsoft.EntityFrameworkCore;
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
