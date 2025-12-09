using TradingApp.Kafka.Configuration;
using TradingApp.Kafka.Extensions;
using TradingApp.MarketAnalytics.Service.Worker.HostedServices;
using TradingApp.MarketAnalytics.Service.Application.Configuration;
using TradingApp.MarketAnalytics.Service.Application.Abstractions;
using TradingApp.MarketAnalytics.Service.Application.Handlers;
using TradingApp.MarketAnalytics.Service.Infrastructure.Kafka;
using TradingApp.Contracts.Events;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaProducerOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddKafkaProducer<string, AggregatedMarketAnalyticsEvent>();
builder.Services.Configure<KafkaTradeConsumerOptions>(builder.Configuration.GetSection("Kafka:Consumer"));
builder.Services.AddSingleton<IRawTradeEventHandler, AggregatingRawTradeEventHandler>();
builder.Services.AddSingleton<IRawTradeEventConsumer, KafkaRawTradeEventConsumer>();
builder.Services.AddHostedService<MarketAnalyticsWorker>();
builder.Services.AddHostedService<RawTradeConsumerHostedService>();

var app = builder.Build();
await app.RunAsync();

