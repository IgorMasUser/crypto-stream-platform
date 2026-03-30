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
            BootstrapServers      = _options.BootstrapServers,
            GroupId               = _options.GroupId,
            AutoOffsetReset       = AutoOffsetReset.Earliest,
            EnableAutoCommit      = true,
            // Offsets are stored manually after successful indexing so a crash
            // mid-handler does not silently advance the committed offset.
            EnableAutoOffsetStore = false
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

                var indexName = _options.IndexPrefix;
                var eventKey  = $"{evt.Symbol}|{evt.WindowStartUtc:O}";

                const int MaxIndexRetries = 3;
                for (var attempt = 0; attempt <= MaxIndexRetries; attempt++)
                {
                    var response = await _elastic.IndexAsync(evt, idx => idx.Index(indexName), stoppingToken);
                    if (response.IsValidResponse)
                    {
                        _logger.LogInformation("Indexed event {EventKey} into {Index}", eventKey, indexName);
                        break;
                    }

                    if (attempt == MaxIndexRetries)
                    {
                        _logger.LogError(
                            "Failed to index event {EventKey} after {MaxRetries} attempts: {Reason}",
                            eventKey, MaxIndexRetries, response.DebugInformation);
                        break;
                    }

                    var delay = TimeSpan.FromMilliseconds(200 * (1 << (attempt + 1)));
                    _logger.LogWarning(
                        "Failed to index event {EventKey} (attempt {Attempt}/{MaxRetries}): {Reason}. Retrying in {DelayMs}ms",
                        eventKey, attempt + 1, MaxIndexRetries, response.DebugInformation, delay.TotalMilliseconds);
                    await Task.Delay(delay, stoppingToken);
                }

                // Advance the committed offset after processing.
                // On persistent ES failure we still store the offset to avoid the
                // consumer getting stuck on an unindexable message forever.
                _consumer.StoreOffset(result!);
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

