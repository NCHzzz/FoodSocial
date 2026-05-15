using System.Text;
using System.Text.Json;
using FoodSocial.Contracts.Identity;
using RabbitMQ.Client;

namespace FoodSocial.Identity.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ publisher for identity integration events.
/// </summary>
public sealed class RabbitMqIdentityIntegrationEventPublisher : IIdentityIntegrationEventPublisher, IDisposable
{
    private readonly ConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    /// <summary>
    /// Initializes a new publisher using the provided RabbitMQ connection string.
    /// </summary>
    /// <param name="connectionString">The RabbitMQ connection string.</param>
    public RabbitMqIdentityIntegrationEventPublisher(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _connectionFactory = new ConnectionFactory
        {
            Uri = new Uri(connectionString)
        };
    }

    /// <inheritdoc />
    public async Task PublishUserProvisionedAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent));

        var properties = new BasicProperties
        {
            MessageId = integrationEvent.EventId.ToString("D"),
            ContentType = "application/json",
            Type = nameof(UserProvisionedIntegrationEvent),
            CorrelationId = integrationEvent.CorrelationId,
            Persistent = true,
            Timestamp = new AmqpTimestamp(integrationEvent.OccurredAtUtc.ToUnixTimeSeconds())
        };

        await _publishLock.WaitAsync(cancellationToken);
        try
        {
            ThrowIfDisposed();
            var channel = await GetChannelAsync(cancellationToken);
            await channel.BasicPublishAsync(
                exchange: "identity.events",
                routingKey: "identity.user.provisioned.v1",
                mandatory: false,
                basicProperties: properties,
                body: payloadBytes,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _publishLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _channel?.Dispose();
        _connection?.Dispose();
        _initializationLock.Dispose();
        _publishLock.Dispose();
        _disposed = true;
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _initializationLock.WaitAsync(cancellationToken);

        try
        {
            ThrowIfDisposed();

            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            _channel?.Dispose();
            _connection?.Dispose();
            _channel = null;
            _connection = null;

            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await _channel.ExchangeDeclareAsync(
                exchange: "identity.events",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            return _channel;
        }
        catch
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _channel = null;
            _connection = null;
            throw;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RabbitMqIdentityIntegrationEventPublisher));
        }
    }
}
