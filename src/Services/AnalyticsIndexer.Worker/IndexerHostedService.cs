using System.Text.Json;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using TradingApp.AnalyticsIndexer.Worker.Options;
using TradingApp.Contracts.Events;

namespace TradingApp.AnalyticsIndexer.Worker;

public sealed class IndexerHostedService : BackgroundService
{
    private readonly ILogger<IndexerHostedService> _logger;
    private readonly ElasticsearchClient _elastic;
    private readonly IndexerOptions _options;
    private readonly IConsumer<string, string> _consumer;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public IndexerHostedService(
        IOptions<IndexerOptions> options,
        ElasticsearchClient elastic,
        ILogger<IndexerHostedService> logger)
    {
        _options = options.Value;
        _elastic = elastic;
        _logger = logger;

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };
        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AnalyticsIndexer.Worker consuming topic {Topic}", _options.Topic);
        _consumer.Subscribe(_options.Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = null;
            try
            {
                result = _consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null)
                {
                    continue;
                }

                var evt = JsonSerializer.Deserialize<AggregatedMarketAnalyticsEvent>(result.Message.Value, _jsonOptions);
                if (evt is null)
                {
                    _logger.LogWarning("Failed to deserialize message at offset {Offset}", result.Offset);
                    continue;
                }

                var indexName = $"{_options.IndexPrefix}";
                var response = await _elastic.IndexAsync(evt, idx => idx.Index(indexName), stoppingToken);
                if (!response.IsValidResponse)
                {
                    _logger.LogWarning("Failed to index event {EventId}: {Reason}", evt.EventId, response.DebugInformation);
                }
                else
                {
                    _logger.LogInformation("Indexed event {EventId} into {Index}", evt.EventId, indexName);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming/indexing message");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        _consumer.Close();
    }

    public override void Dispose()
    {
        base.Dispose();
        _consumer.Dispose();
    }
}

