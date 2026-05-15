using System.Text;
using RabbitMQ.Client;

namespace FoodSocial.Messaging;

public sealed class RabbitMqIntegrationEventPublisher : IIntegrationEventPublisher, IDisposable
{
    private readonly ConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly HashSet<string> _declaredExchanges = new(StringComparer.Ordinal);
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    public RabbitMqIntegrationEventPublisher(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _connectionFactory = new ConnectionFactory
        {
            Uri = new Uri(connectionString)
        };
    }

    public async Task PublishAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var channel = await GetChannelAsync(cancellationToken);
        await EnsureExchangeDeclaredAsync(channel, message.ExchangeName, cancellationToken);

        var properties = new BasicProperties
        {
            MessageId = message.EventId.ToString("D"),
            ContentType = "application/json",
            Type = message.EventType,
            CorrelationId = message.CorrelationId,
            Persistent = true,
            Timestamp = new AmqpTimestamp(message.OccurredAtUtc.ToUnixTimeSeconds()),
            Headers = new Dictionary<string, object?>
            {
                ["event-version"] = message.EventVersion,
                ["aggregate-id"] = message.AggregateId.ToString("D"),
                ["aggregate-type"] = message.AggregateType
            }
        };

        await channel.BasicPublishAsync(
            exchange: message.ExchangeName,
            routingKey: message.RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(message.PayloadJson),
            cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _channel?.Dispose();
        _connection?.Dispose();
        _initializationLock.Dispose();
        _disposed = true;
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_channel is not null)
        {
            return _channel;
        }

        await _initializationLock.WaitAsync(cancellationToken);

        try
        {
            ThrowIfDisposed();

            if (_channel is not null)
            {
                return _channel;
            }

            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
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

    private async Task EnsureExchangeDeclaredAsync(IChannel channel, string exchangeName, CancellationToken cancellationToken)
    {
        if (_declaredExchanges.Contains(exchangeName))
        {
            return;
        }

        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        _declaredExchanges.Add(exchangeName);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RabbitMqIntegrationEventPublisher));
        }
    }
}
