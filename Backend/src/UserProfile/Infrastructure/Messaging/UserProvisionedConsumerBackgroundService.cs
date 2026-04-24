using System.Text.Json;
using FoodSocial.Contracts.Identity;
using FoodSocial.Messaging;
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
    private const int MaxRetryAttempts = 3;
    private static readonly RabbitMqBoundedRetryTopology RetryTopology = new(
        MainExchangeName: "identity.events",
        QueueName,
        RoutingKey,
        RetryExchangeName,
        RetryQueueName,
        RetryRoutingKey,
        DeadLetterExchangeName,
        DeadLetterQueueName,
        DeadLetterRoutingKey,
        RetryDelayMilliseconds: 500);
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

        await RabbitMqBoundedRetryTopologyConfigurator.DeclareAsync(channel, RetryTopology, stoppingToken);

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
        var retryCount = RabbitMqBoundedRetryTopologyConfigurator.GetRetryCount(eventArgs.BasicProperties);

        try
        {
            if (retryCount >= MaxRetryAttempts)
            {
                _logger.LogError(
                    exception,
                    "UserProfile bootstrap consumer dead-lettered delivery tag {DeliveryTag} after {RetryCount} retries.",
                    eventArgs.DeliveryTag,
                    retryCount);

                await RabbitMqBoundedRetryTopologyConfigurator.PublishDeadLetterAsync(
                    channel,
                    RetryTopology,
                    eventArgs,
                    cancellationToken);
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            var nextRetryCount = retryCount + 1;
            await RabbitMqBoundedRetryTopologyConfigurator.PublishRetryAsync(
                channel,
                RetryTopology,
                eventArgs,
                nextRetryCount,
                cancellationToken);

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
}
