namespace TradingApp.Elastic.Configuration;

public sealed class ElasticOptions
{
    public string Uri { get; set; } = "http://localhost:9200";
    public string? ApiKey { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}

