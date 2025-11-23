using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingApp.MarketData.Ingestor.Application.Abstractions;
using TradingApp.MarketData.Ingestor.Domain.Models;

namespace TradingApp.MarketData.Ingestor.Infrastructure.Binance;

public sealed class BinanceTradeStream : IBinanceTradeStream, IAsyncDisposable
{
    private readonly BinanceStreamOptions _options;
    private readonly ILogger<BinanceTradeStream> _logger;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false
    };

    public BinanceTradeStream(IOptions<BinanceStreamOptions> options, ILogger<BinanceTradeStream> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<BinanceCombinedTradeMessage> SubscribeAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_options.Symbols.ToList().Count == 0)
        {
            throw new InvalidOperationException("At least one Binance symbol must be configured.");
        }

        var streamUri = BuildStreamUri();
        var consecutiveFailures = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            using var socket = new ClientWebSocket();
            IAsyncEnumerable<BinanceCombinedTradeMessage>? streamMessages = null;

            try
            {
                _logger.LogInformation("Connecting to Binance stream {Stream}", streamUri);
                await socket.ConnectAsync(streamUri, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Connected to Binance stream {Stream}", streamUri);

                consecutiveFailures = 0;
                streamMessages = ReceiveInternalAsync(socket, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            catch (Exception ex)
            {
                consecutiveFailures++;
                var backoff = CalculateReconnectDelay(consecutiveFailures);
                _logger.LogWarning(
                    ex,
                    "Binance stream connection dropped. Reconnecting in {Delay}s (attempt {Attempt})",
                    backoff.TotalSeconds,
                    consecutiveFailures);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
                }

                continue;
            }

            if (streamMessages is not null)
            {
                await foreach (var message in streamMessages.WithCancellation(cancellationToken))
                {
                    yield return message;
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                consecutiveFailures++;
                var backoff = CalculateReconnectDelay(consecutiveFailures);
                _logger.LogInformation(
                    "Binance stream closed. Attempting reconnect in {Delay}s (attempt {Attempt})",
                    backoff.TotalSeconds,
                    consecutiveFailures);

                await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async IAsyncEnumerable<BinanceCombinedTradeMessage> ReceiveInternalAsync(
        ClientWebSocket socket,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(_options.ReceiveBufferSize);
        var memory = new ArraySegment<byte>(buffer);
        using var messageBuffer = new MemoryStream();

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(memory, cancellationToken).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken).ConfigureAwait(false);
                    break;
                }

                messageBuffer.Write(memory.Array!, memory.Offset, result.Count);

                if (result.EndOfMessage)
                {
                    var payload = messageBuffer.ToArray();
                    messageBuffer.SetLength(0);

                    if (payload.Length == 0)
                    {
                        continue;
                    }

                    var message = Deserialize(payload);
                    if (message is not null)
                    {
                        yield return message;
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private BinanceCombinedTradeMessage? Deserialize(byte[] payload)
    {
        try
        {
            return JsonSerializer.Deserialize<BinanceCombinedTradeMessage>(payload, _serializerOptions);
        }
        catch (JsonException ex)
        {
            var preview = Encoding.UTF8.GetString(payload, 0, Math.Min(payload.Length, 200));
            _logger.LogWarning(ex, "Failed to deserialize Binance trade payload: {Payload}", preview);
            return null;
        }
    }

    private Uri BuildStreamUri()
    {
        var normalizedSymbols = _options.Symbols
            .Select(symbol => symbol.Trim().ToLowerInvariant())
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => $"{symbol}@trade")
            .ToArray();

        if (normalizedSymbols.Length == 0)
        {
            throw new InvalidOperationException("At least one valid Binance symbol must be configured.");
        }

        var endpoint = _options.Endpoint.TrimEnd('/');
        var uri = $"{endpoint}?streams={string.Join('/', normalizedSymbols)}";
        return new Uri(uri, UriKind.Absolute);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private TimeSpan CalculateReconnectDelay(int failureCount)
    {
        var baseDelay = TimeSpan.FromSeconds(_options.ReconnectDelaySeconds);
        var maxDelay = TimeSpan.FromSeconds(_options.MaxReconnectDelaySeconds);
        var exponential = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, Math.Clamp(failureCount - 1, 0, 10)));
        var capped = exponential > maxDelay ? maxDelay : exponential;
        var jitterSeconds = _options.ReconnectJitterSeconds > 0
            ? Random.Shared.NextDouble() * _options.ReconnectJitterSeconds
            : 0;
        return capped + TimeSpan.FromSeconds(jitterSeconds);
    }
}

