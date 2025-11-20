using TradingApp.Kafka.Configuration;
using TradingApp.Kafka.Extensions;
using TradingApp.MarketAnalytics.Service.Worker.HostedServices;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaProducerOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddKafkaProducer<string, string>();
builder.Services.AddHostedService<MarketAnalyticsWorker>();

var app = builder.Build();
await app.RunAsync();

