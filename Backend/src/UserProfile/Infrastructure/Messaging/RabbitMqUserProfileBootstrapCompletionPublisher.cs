using System.Text;
using System.Text.Json;
using FoodSocial.Contracts.Identity;
using RabbitMQ.Client;

namespace FoodSocial.UserProfile.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ publisher for explicit UserProfile bootstrap completion events.
/// </summary>
public sealed class RabbitMqUserProfileBootstrapCompletionPublisher : IUserProfileBootstrapCompletionPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqUserProfileBootstrapCompletionPublisher"/> class.
    /// </summary>
    /// <param name="connectionString">The RabbitMQ connection string.</param>
    public RabbitMqUserProfileBootstrapCompletionPublisher(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString)
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync(
                exchange: "identity.events",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc />
    public async Task PublishAsync(UserProfileBootstrapCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        ThrowIfDisposed();

        var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent));
        var properties = new BasicProperties
        {
            MessageId = integrationEvent.EventId.ToString("D"),
            ContentType = "application/json",
            Type = nameof(UserProfileBootstrapCompletedIntegrationEvent),
            CorrelationId = integrationEvent.CorrelationId,
            Persistent = true,
            Timestamp = new AmqpTimestamp(integrationEvent.OccurredAtUtc.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: "identity.events",
            routingKey: "identity.bootstrap.completed.v2",
            mandatory: false,
            basicProperties: properties,
            body: payloadBytes,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _channel.Dispose();
        _connection.Dispose();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RabbitMqUserProfileBootstrapCompletionPublisher));
        }
    }
}
