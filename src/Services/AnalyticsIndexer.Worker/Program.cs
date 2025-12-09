using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TradingApp.AnalyticsIndexer.Worker;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(builder =>
            {
                builder.AddConsole();
            })
            .ConfigureServices(services =>
            {
                services.AddHostedService<IndexerHostedService>();
            })
            .Build();

        await host.RunAsync();
    }
}

