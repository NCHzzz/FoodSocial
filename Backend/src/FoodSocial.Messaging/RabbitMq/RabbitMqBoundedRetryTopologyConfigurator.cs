using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FoodSocial.Messaging;

/// <summary>
/// Applies a standard RabbitMQ bounded-retry topology and retry-header handling.
/// </summary>
public static class RabbitMqBoundedRetryTopologyConfigurator
{
    private const string RetryCountHeaderName = "x-retry-count";

    public static async Task DeclareAsync(
        IChannel channel,
        RabbitMqBoundedRetryTopology topology,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(topology);

        await channel.ExchangeDeclareAsync(
            exchange: topology.MainExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: topology.RetryExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: topology.DeadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: topology.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = topology.DeadLetterExchangeName,
                ["x-dead-letter-routing-key"] = topology.DeadLetterRoutingKey,
            },
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: topology.RetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = topology.MainExchangeName,
                ["x-dead-letter-routing-key"] = topology.RoutingKey,
                ["x-message-ttl"] = topology.RetryDelayMilliseconds,
            },
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: topology.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: topology.QueueName,
            exchange: topology.MainExchangeName,
            routingKey: topology.RoutingKey,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: topology.RetryQueueName,
            exchange: topology.RetryExchangeName,
            routingKey: topology.RetryRoutingKey,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: topology.DeadLetterQueueName,
            exchange: topology.DeadLetterExchangeName,
            routingKey: topology.DeadLetterRoutingKey,
            cancellationToken: cancellationToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);
    }

    public static async Task PublishRetryAsync(
        IChannel channel,
        RabbitMqBoundedRetryTopology topology,
        BasicDeliverEventArgs eventArgs,
        int retryCount,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(eventArgs);

        var headers = CloneHeaders(eventArgs.BasicProperties.Headers);
        headers[RetryCountHeaderName] = retryCount;

        var properties = new BasicProperties
        {
            Persistent = eventArgs.BasicProperties.Persistent,
            ContentType = eventArgs.BasicProperties.ContentType,
            ContentEncoding = eventArgs.BasicProperties.ContentEncoding,
            CorrelationId = eventArgs.BasicProperties.CorrelationId,
            MessageId = eventArgs.BasicProperties.MessageId,
            Type = eventArgs.BasicProperties.Type,
            Timestamp = eventArgs.BasicProperties.Timestamp,
            Headers = headers,
        };

        await channel.BasicPublishAsync(
            exchange: topology.RetryExchangeName,
            routingKey: topology.RetryRoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: eventArgs.Body,
            cancellationToken: cancellationToken);
    }

    public static async Task PublishDeadLetterAsync(
        IChannel channel,
        RabbitMqBoundedRetryTopology topology,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(eventArgs);

        var properties = new BasicProperties
        {
            Persistent = eventArgs.BasicProperties.Persistent,
            ContentType = eventArgs.BasicProperties.ContentType,
            ContentEncoding = eventArgs.BasicProperties.ContentEncoding,
            CorrelationId = eventArgs.BasicProperties.CorrelationId,
            MessageId = eventArgs.BasicProperties.MessageId,
            Type = eventArgs.BasicProperties.Type,
            Timestamp = eventArgs.BasicProperties.Timestamp,
            Headers = CloneHeaders(eventArgs.BasicProperties.Headers),
        };

        await channel.BasicPublishAsync(
            exchange: topology.DeadLetterExchangeName,
            routingKey: topology.DeadLetterRoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: eventArgs.Body,
            cancellationToken: cancellationToken);
    }

    public static int GetRetryCount(IReadOnlyBasicProperties? basicProperties)
    {
        if (basicProperties?.Headers is null
            || !basicProperties.Headers.TryGetValue(RetryCountHeaderName, out var value)
            || value is null)
        {
            return 0;
        }

        return value switch
        {
            byte retryByte => retryByte,
            sbyte retrySByte => retrySByte,
            short retryShort => retryShort,
            ushort retryUShort => (int)retryUShort,
            int retryInt => retryInt,
            uint retryUInt => (int)Math.Min(retryUInt, (uint)int.MaxValue),
            long retryLong => (int)Math.Min(retryLong, int.MaxValue),
            ulong retryULong => (int)Math.Min(retryULong, (ulong)int.MaxValue),
            byte[] retryBytes when int.TryParse(Encoding.UTF8.GetString(retryBytes), out var parsedRetryCount) => Math.Clamp(parsedRetryCount, 0, int.MaxValue),
            _ => 0,
        };
    }

    private static Dictionary<string, object?> CloneHeaders(IDictionary<string, object?>? headers)
    {
        var clone = new Dictionary<string, object?>(StringComparer.Ordinal);

        if (headers is null)
        {
            return clone;
        }

        foreach (var (key, value) in headers)
        {
            clone[key] = value;
        }

        return clone;
    }
}
