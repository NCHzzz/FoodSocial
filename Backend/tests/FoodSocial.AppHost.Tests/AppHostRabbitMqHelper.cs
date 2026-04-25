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

        await EnsureUserProvisionedTopologyAsync(cancellationToken);

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

        await EnsureBootstrapCompletedTopologyAsync(cancellationToken);

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

        await EnsureUserProvisionedTopologyAsync(cancellationToken);
        await EnsureBootstrapCompletedTopologyAsync(cancellationToken);

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

    private async Task EnsureUserProvisionedTopologyAsync(CancellationToken cancellationToken)
    {
        await EnsureBoundedRetryTopologyAsync(
            mainExchangeName: "identity.events",
            queueName: "userprofile.bootstrap.user-provisioned.v2",
            routingKey: "identity.user.provisioned.v1",
            retryExchangeName: "userprofile.bootstrap.user-provisioned.retry.v2",
            retryQueueName: "userprofile.bootstrap.user-provisioned.retry.v2",
            retryRoutingKey: "userprofile.bootstrap.user-provisioned.retry.v2",
            deadLetterExchangeName: "userprofile.bootstrap.user-provisioned.dead-letter.v2",
            deadLetterQueueName: "userprofile.bootstrap.user-provisioned.dead-letter.v2",
            deadLetterRoutingKey: "userprofile.bootstrap.user-provisioned.dead-letter.v2",
            cancellationToken);
    }

    private async Task EnsureBootstrapCompletedTopologyAsync(CancellationToken cancellationToken)
    {
        await EnsureBoundedRetryTopologyAsync(
            mainExchangeName: "identity.events",
            queueName: "identity.bootstrap.completed.v2",
            routingKey: "identity.bootstrap.completed.v2",
            retryExchangeName: "identity.bootstrap.completed.retry.v2",
            retryQueueName: "identity.bootstrap.completed.retry.v2",
            retryRoutingKey: "identity.bootstrap.completed.retry.v2",
            deadLetterExchangeName: "identity.bootstrap.completed.dead-letter.v2",
            deadLetterQueueName: "identity.bootstrap.completed.dead-letter.v2",
            deadLetterRoutingKey: "identity.bootstrap.completed.dead-letter.v2",
            cancellationToken);
    }

    private async Task EnsureBoundedRetryTopologyAsync(
        string mainExchangeName,
        string queueName,
        string routingKey,
        string retryExchangeName,
        string retryQueueName,
        string retryRoutingKey,
        string deadLetterExchangeName,
        string deadLetterQueueName,
        string deadLetterRoutingKey,
        CancellationToken cancellationToken)
    {
        await _channel.ExchangeDeclareAsync(
            exchange: mainExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: retryExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: deadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = deadLetterExchangeName,
                ["x-dead-letter-routing-key"] = deadLetterRoutingKey,
            },
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: retryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = mainExchangeName,
                ["x-dead-letter-routing-key"] = routingKey,
                ["x-message-ttl"] = 500,
            },
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: deadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: mainExchangeName,
            routingKey: routingKey,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: retryQueueName,
            exchange: retryExchangeName,
            routingKey: retryRoutingKey,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: deadLetterQueueName,
            exchange: deadLetterExchangeName,
            routingKey: deadLetterRoutingKey,
            cancellationToken: cancellationToken);
    }
}
