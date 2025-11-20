using System.Globalization;
using TradingApp.Contracts.Events;
using TradingApp.Kafka.Abstractions;
using TradingApp.MarketData.Ingestor.Application.Abstractions;
using TradingApp.MarketData.Ingestor.Application.Configuration;
using TradingApp.MarketData.Ingestor.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TradingApp.MarketData.Ingestor.Application.Services;

public sealed class TradeIngestionPipeline : ITradeIngestionPipeline
{
    private readonly IKafkaProducer<string, RawMarketTradeEvent> _producer;
    private readonly MarketDataIngestorOptions _options;
    private readonly ILogger<TradeIngestionPipeline> _logger;

    public TradeIngestionPipeline(
        IKafkaProducer<string, RawMarketTradeEvent> producer,
        IOptions<MarketDataIngestorOptions> options,
        ILogger<TradeIngestionPipeline> logger)
    {
        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task HandleAsync(BinanceCombinedTradeMessage message, CancellationToken cancellationToken)
    {
        var tradeEvent = MapToContract(message);

        await _producer.ProduceAsync(_options.KafkaTopic, tradeEvent.Symbol, tradeEvent, cancellationToken)
            .ConfigureAwait(false);
    }

    private RawMarketTradeEvent MapToContract(BinanceCombinedTradeMessage message)
    {
        var data = message.Data;

        try
        {
            return new RawMarketTradeEvent(
                Guid.NewGuid().ToString("N"),
                "binance",
                data.Symbol,
                message.Stream,
                ParseDecimal(data.Price),
                ParseDecimal(data.Quantity),
                data.TradeId,
                DateTimeOffset.FromUnixTimeMilliseconds(data.EventTime).UtcDateTime,
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to transform Binance trade message for symbol {Symbol}",
                data.Symbol);
            throw;
        }
    }

    private static decimal ParseDecimal(string value) =>
        decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
}

