using System.Text;
using System.Text.Json;
using FoodSocial.Contracts.Identity;
using FoodSocial.UserProfile.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FoodSocial.UserProfile.Infrastructure.Messaging;

/// <summary>
/// Consumes identity bootstrap events and creates minimal profile shells idempotently.
/// </summary>
public sealed class UserProvisionedConsumerBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration,
    ILogger<UserProvisionedConsumerBackgroundService> logger)
    : BackgroundService
{
    internal const string QueueName = "userprofile.bootstrap.user-provisioned.v2";
    internal const string RoutingKey = "identity.user.provisioned.v1";
    internal const string RetryExchangeName = "userprofile.bootstrap.user-provisioned.retry.v2";
    internal const string RetryQueueName = "userprofile.bootstrap.user-provisioned.retry.v2";
    internal const string RetryRoutingKey = "userprofile.bootstrap.user-provisioned.retry.v2";
    internal const string DeadLetterExchangeName = "userprofile.bootstrap.user-provisioned.dead-letter.v2";
    internal const string DeadLetterQueueName = "userprofile.bootstrap.user-provisioned.dead-letter.v2";
    internal const string DeadLetterRoutingKey = "userprofile.bootstrap.user-provisioned.dead-letter.v2";
    private const string MainExchangeName = "identity.events";
    private const string RetryCountHeaderName = "x-retry-count";
    private const int MaxRetryAttempts = 3;
    private const int RetryDelayMilliseconds = 500;
    private static readonly TimeSpan ConnectionRetryDelay = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ILogger<UserProvisionedConsumerBackgroundService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitMqConnectionString = _configuration.GetConnectionString("rabbitmq")
            ?? _configuration["ConnectionStrings:rabbitmq"]
            ?? _configuration["RabbitMQ:ConnectionString"];

        if (string.IsNullOrWhiteSpace(rabbitMqConnectionString))
        {
            _logger.LogWarning("UserProfile bootstrap consumer is disabled because RabbitMQ is not configured.");
            return;
        }

        var factory = new ConnectionFactory
        {
            Uri = new Uri(rabbitMqConnectionString)
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerLoopAsync(factory, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "UserProfile bootstrap consumer connection loop failed. Retrying after delay.");
                await Task.Delay(ConnectionRetryDelay, stoppingToken);
            }
        }
    }

    private async Task RunConsumerLoopAsync(ConnectionFactory factory, CancellationToken stoppingToken)
    {
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: MainExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: RetryExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchangeName,
                ["x-dead-letter-routing-key"] = DeadLetterRoutingKey
            },
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: RetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = MainExchangeName,
                ["x-dead-letter-routing-key"] = RoutingKey,
                ["x-message-ttl"] = RetryDelayMilliseconds
            },
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: MainExchangeName,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: RetryQueueName,
            exchange: RetryExchangeName,
            routingKey: RetryRoutingKey,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: DeadLetterQueueName,
            exchange: DeadLetterExchangeName,
            routingKey: DeadLetterRoutingKey,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var integrationEvent = JsonSerializer.Deserialize<UserProvisionedIntegrationEvent>(eventArgs.Body.ToArray())
                    ?? throw new InvalidOperationException("The identity bootstrap payload could not be deserialized.");

                await HandleMessageAsync(integrationEvent, stoppingToken);
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                await HandleFailureAsync(channel, eventArgs, exception, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task HandleMessageAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var bootstrapService = scope.ServiceProvider.GetRequiredService<UserProfileBootstrapService>();
        var completionPublisher = scope.ServiceProvider.GetRequiredService<IUserProfileBootstrapCompletionPublisher>();

        var bootstrapResult = await bootstrapService.EnsureShellAsync(integrationEvent, cancellationToken);
        var completionEvent = new UserProfileBootstrapCompletedIntegrationEvent(
            EventId: Guid.NewGuid(),
            IdentityId: bootstrapResult.Shell.IdentityId,
            Subject: bootstrapResult.Shell.Subject,
            ProfileShellId: bootstrapResult.Shell.Id,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            EventVersion: 1,
            CorrelationId: integrationEvent.CorrelationId,
            CausationId: integrationEvent.EventId.ToString("D"));

        await completionPublisher.PublishAsync(completionEvent, cancellationToken);
    }

    private async Task HandleFailureAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var retryCount = GetRetryCount(eventArgs.BasicProperties);

        try
        {
            if (retryCount >= MaxRetryAttempts)
            {
                _logger.LogError(
                    exception,
                    "UserProfile bootstrap consumer dead-lettered delivery tag {DeliveryTag} after {RetryCount} retries.",
                    eventArgs.DeliveryTag,
                    retryCount);

                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
                return;
            }

            var nextRetryCount = retryCount + 1;
            await PublishRetryAsync(channel, eventArgs, nextRetryCount, cancellationToken);

            _logger.LogWarning(
                exception,
                "UserProfile bootstrap consumer scheduled retry {RetryCount}/{MaxRetryAttempts} for delivery tag {DeliveryTag}.",
                nextRetryCount,
                MaxRetryAttempts,
                eventArgs.DeliveryTag);

            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
        catch (Exception retryException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                retryException,
                "UserProfile bootstrap consumer failed to apply bounded retry handling for delivery tag {DeliveryTag}; requeueing original delivery.",
                eventArgs.DeliveryTag);

            await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken: cancellationToken);
        }
    }

    private static async Task PublishRetryAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        int retryCount,
        CancellationToken cancellationToken)
    {
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
            Headers = headers
        };

        await channel.BasicPublishAsync(
            exchange: RetryExchangeName,
            routingKey: RetryRoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: eventArgs.Body,
            cancellationToken: cancellationToken);
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

    private static int GetRetryCount(IReadOnlyBasicProperties? basicProperties)
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
            ushort retryUShort => retryUShort,
            int retryInt => retryInt,
            uint retryUInt => (int)retryUInt,
            long retryLong => (int)retryLong,
            ulong retryULong => (int)retryULong,
            byte[] retryBytes when int.TryParse(Encoding.UTF8.GetString(retryBytes), out var parsedRetryCount) => parsedRetryCount,
            _ => 0
        };
    }
}
