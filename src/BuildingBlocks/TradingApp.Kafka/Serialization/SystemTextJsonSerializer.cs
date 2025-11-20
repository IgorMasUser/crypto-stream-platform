using System.Text.Json;
using Confluent.Kafka;

namespace TradingApp.Kafka.Serialization;

internal sealed class SystemTextJsonSerializer<T> : ISerializer<T>
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public byte[] Serialize(T data, SerializationContext context)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data, Options);
    }
}

