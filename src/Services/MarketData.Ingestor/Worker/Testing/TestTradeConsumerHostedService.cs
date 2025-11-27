using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using TradingApp.Contracts.Events;
using TradingApp.Kafka.Configuration;
using TradingApp.MarketData.Ingestor.Application.Configuration;

namespace TradingApp.MarketData.Ingestor.Worker.Testing;

/// <summary>
/// Development-only hosted service that consumes a handful of messages from Kafka
/// and logs them for manual verification.
/// </summary>
public sealed class TestTradeConsumerHostedService : BackgroundService
{
    private readonly KafkaProducerOptions _kafkaOptions;
    private readonly TestTradeConsumerOptions _consumerOptions;
    private readonly ILogger<TestTradeConsumerHostedService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public TestTradeConsumerHostedService(
        IOptions<KafkaProducerOptions> kafkaOptions,
        IOptions<TestTradeConsumerOptions> consumerOptions,
        ILogger<TestTradeConsumerHostedService> logger)
    {
        _kafkaOptions = kafkaOptions.Value;
        _consumerOptions = consumerOptions.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_consumerOptions.Enabled)
        {
            _logger.LogInformation("Test trade consumer is disabled via configuration.");
            return Task.CompletedTask;
        }

        if (!_kafkaOptions.Enabled || string.IsNullOrWhiteSpace(_kafkaOptions.BootstrapServers))
        {
            _logger.LogWarning("Kafka is disabled or not configured. Test consumer will not start.");
            return Task.CompletedTask;
        }

        return ConsumeAsync(stoppingToken);
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = string.IsNullOrWhiteSpace(_consumerOptions.GroupId)
                ? "marketdata-ingestor-test-consumer"
                : _consumerOptions.GroupId,
            EnableAutoCommit = _consumerOptions.EnableAutoCommit,
            AutoOffsetReset = _consumerOptions.AutoOffsetReset,
            ClientId = $"{_kafkaOptions.ClientId ?? Environment.MachineName}-test-consumer",
            AllowAutoCreateTopics = false
        };

        using var consumer = new ConsumerBuilder<string, RawMarketTradeEvent>(config)
            .SetValueDeserializer(new RawMarketTradeEventDeserializer())
            .SetErrorHandler((_, error) =>
            {
                if (error.IsFatal)
                {
                    _logger.LogError("Kafka test consumer fatal error: {Reason}", error.Reason);
                }
                else
                {
                    _logger.LogWarning("Kafka test consumer warning: {Reason}", error.Reason);
                }
            })
            .Build();

        _logger.LogInformation(
            "Test trade consumer subscribing to topic {Topic} with group {GroupId}.",
            _consumerOptions.Topic,
            config.GroupId);

        consumer.Subscribe(_consumerOptions.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result?.Message?.Value is null)
                    {
                        continue;
                    }

                    var trade = result.Message.Value;
                    _logger.LogInformation(
                        "Test consumer received trade {TradeId} for {Symbol} at {Price} ({Quantity} units). EventId={EventId}",
                        trade.TradeId,
                        trade.Symbol,
                        trade.Price,
                        trade.Quantity,
                        trade.EventId);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume exception on topic {Topic}", _consumerOptions.Topic);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Test trade consumer shutting down.");
        }

        await Task.CompletedTask;
    }

    private sealed class RawMarketTradeEventDeserializer : IDeserializer<RawMarketTradeEvent>
    {
        public RawMarketTradeEvent Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (isNull || data.IsEmpty)
            {
                throw new InvalidOperationException("Kafka message payload was empty.");
            }

            return JsonSerializer.Deserialize<RawMarketTradeEvent>(data, JsonOptions)
                ?? throw new JsonException("Unable to deserialize RawMarketTradeEvent from Kafka payload.");
        }
    }
}


