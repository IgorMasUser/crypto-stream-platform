using TradingApp.Elastic.Configuration;
using TradingApp.Elastic.Extensions;
using TradingApp.AnalyticsIndexer.Worker;
using TradingApp.AnalyticsIndexer.Worker.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddLogging(b => b.AddConsole());

builder.Services.Configure<ElasticOptions>(builder.Configuration.GetSection("Elastic"));
builder.Services.Configure<IndexerOptions>(builder.Configuration.GetSection("Indexer"));
builder.Services.AddElasticClient();
builder.Services.AddHostedService<IndexerHostedService>();

var app = builder.Build();
await app.RunAsync();

