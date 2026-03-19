using TradingApp.Elastic.Extensions;
using TradingApp.Elastic.Configuration;
using TradingApp.Contracts.Events;
using Elastic.Clients.Elasticsearch;
using TradingApp.Dashboard.Web.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.Configure<ElasticOptions>(builder.Configuration.GetSection("Elastic"));
builder.Services.AddElasticClient();
builder.Services.AddSingleton<AnalyticsService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapGet("/api/analytics", async (ElasticsearchClient client, IConfiguration config, CancellationToken ct) =>
{
    var index = config.GetValue<string>("Elastic:IndexPrefix") ?? "analytics-index";
    var resp = await client.SearchAsync<AggregatedMarketAnalyticsEvent>(s => s
        .Index(index)
        .Size(20), ct);

    return Results.Ok(resp.Documents);
});

app.Run();

