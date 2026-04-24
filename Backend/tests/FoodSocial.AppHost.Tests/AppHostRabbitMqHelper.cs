using System.Text;
using System.Text.Json;
using FoodSocial.Contracts.Identity;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace FoodSocial.AppHost.Tests;

internal sealed class AppHostRabbitMqHelper : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public AppHostRabbitMqHelper(string connectionString)
    {
        _connection = CreateConnection(connectionString);
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async Task PublishUserProvisionedAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        await _channel.ExchangeDeclareAsync(
            exchange: "identity.events",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent));
        var properties = new BasicProperties
        {
            MessageId = integrationEvent.EventId.ToString("D"),
            Type = nameof(UserProvisionedIntegrationEvent),
            ContentType = "application/json",
            Persistent = true,
            CorrelationId = integrationEvent.CorrelationId,
            Timestamp = new AmqpTimestamp(integrationEvent.OccurredAtUtc.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: "identity.events",
            routingKey: "identity.user.provisioned.v1",
            mandatory: false,
            basicProperties: properties,
            body: payload,
            cancellationToken: cancellationToken);
    }

    public async Task PublishUserProfileBootstrapCompletedAsync(UserProfileBootstrapCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        await _channel.ExchangeDeclareAsync(
            exchange: "identity.events",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent));
        var properties = new BasicProperties
        {
            MessageId = integrationEvent.EventId.ToString("D"),
            Type = nameof(UserProfileBootstrapCompletedIntegrationEvent),
            ContentType = "application/json",
            Persistent = true,
            CorrelationId = integrationEvent.CorrelationId,
            Timestamp = new AmqpTimestamp(integrationEvent.OccurredAtUtc.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: "identity.events",
            routingKey: "identity.bootstrap.completed.v2",
            mandatory: false,
            basicProperties: properties,
            body: payload,
            cancellationToken: cancellationToken);
    }

    public async Task<long> GetMessageCountAsync(string queueName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        var declareOk = await _channel.QueueDeclarePassiveAsync(queueName, cancellationToken);
        return declareOk.MessageCount;
    }

    public async Task PurgeQueuesIfPresentAsync(IEnumerable<string> queueNames, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queueNames);

        foreach (var queueName in queueNames)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

            try
            {
                await _channel.QueuePurgeAsync(queueName, cancellationToken);
            }
            catch (OperationInterruptedException exception) when (exception.ShutdownReason?.ReplyCode == 404)
            {
                // Some bootstrap queues are declared by hosted consumers after startup; missing queues have no state to purge.
            }
        }
    }

    public async Task<T?> ReadSingleMessageAsync<T>(string queueName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        var response = await _channel.BasicGetAsync(queueName, autoAck: false, cancellationToken: cancellationToken);
        if (response is null)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(response.Body.ToArray());
        }
        finally
        {
            await _channel.BasicNackAsync(response.DeliveryTag, multiple: false, requeue: true, cancellationToken: cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static IConnection CreateConnection(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString)
        };

        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
    }
}
