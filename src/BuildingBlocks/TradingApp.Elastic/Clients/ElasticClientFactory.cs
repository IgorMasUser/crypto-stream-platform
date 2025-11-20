using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingApp.Elastic.Configuration;

namespace TradingApp.Elastic.Clients;

public sealed class ElasticClientFactory
{
    private readonly ILogger<ElasticClientFactory> _logger;
    private readonly ElasticOptions _options;

    public ElasticClientFactory(IOptions<ElasticOptions> options, ILogger<ElasticClientFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public ElasticsearchClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_options.Uri))
        {
            throw new InvalidOperationException("Elastic URI must be configured.");
        }

        var settings = new ElasticsearchClientSettings(new Uri(_options.Uri));

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            settings.Authentication(new ApiKey(_options.ApiKey));
        }
        else if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            settings.Authentication(new BasicAuthentication(_options.Username, _options.Password));
        }

        _logger.LogInformation("Creating Elasticsearch client targeting {Uri}", _options.Uri);
        return new ElasticsearchClient(settings);
    }
}

