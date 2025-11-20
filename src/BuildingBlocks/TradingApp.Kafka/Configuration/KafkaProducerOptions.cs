using Confluent.Kafka;

namespace TradingApp.Kafka.Configuration;

public sealed class KafkaProducerOptions
{
    public bool Enabled { get; set; } = true;
    public string BootstrapServers { get; set; } = string.Empty;
    public string? ClientId { get; set; } = Environment.MachineName;
    public SecurityProtocol? SecurityProtocol { get; set; }
    public SaslMechanism? SaslMechanism { get; set; }
    public string? SaslUsername { get; set; }
    public string? SaslPassword { get; set; }
    public Acks? Acks { get; set; } = Confluent.Kafka.Acks.Leader;

    public ProducerConfig BuildProducerConfig()
    {
        if (string.IsNullOrWhiteSpace(BootstrapServers))
        {
            throw new InvalidOperationException("Kafka bootstrap servers must be configured.");
        }

        var config = new ProducerConfig
        {
            BootstrapServers = BootstrapServers,
            ClientId = ClientId
        };

        if (SecurityProtocol.HasValue)
        {
            config.SecurityProtocol = SecurityProtocol.Value;
        }

        if (SaslMechanism.HasValue)
        {
            config.SaslMechanism = SaslMechanism.Value;
        }

        if (!string.IsNullOrWhiteSpace(SaslUsername))
        {
            config.SaslUsername = SaslUsername;
        }

        if (!string.IsNullOrWhiteSpace(SaslPassword))
        {
            config.SaslPassword = SaslPassword;
        }

        if (Acks.HasValue)
        {
            config.Acks = Acks.Value;
        }

        return config;
    }
}

