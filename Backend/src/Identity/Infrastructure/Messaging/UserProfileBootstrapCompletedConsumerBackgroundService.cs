using System.Text.Json;
using FoodSocial.Contracts.Identity;
using FoodSocial.Identity.Application;
using FoodSocial.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FoodSocial.Identity.Infrastructure.Messaging;

/// <summary>
/// Consumes explicit UserProfile bootstrap completion signals and activates pending identities.
/// </summary>
public sealed class UserProfileBootstrapCompletedConsumerBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration,
    ILogger<UserProfileBootstrapCompletedConsumerBackgroundService> logger)
    : BackgroundService
{
    internal const string QueueName = "identity.bootstrap.completed.v2";
    internal const string RoutingKey = "identity.bootstrap.completed.v2";
    internal const string RetryExchangeName = "identity.bootstrap.completed.retry.v2";
    internal const string RetryQueueName = "identity.bootstrap.completed.retry.v2";
    internal const string RetryRoutingKey = "identity.bootstrap.completed.retry.v2";
    internal const string DeadLetterExchangeName = "identity.bootstrap.completed.dead-letter.v2";
    internal const string DeadLetterQueueName = "identity.bootstrap.completed.dead-letter.v2";
    internal const string DeadLetterRoutingKey = "identity.bootstrap.completed.dead-letter.v2";
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

    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ILogger<UserProfileBootstrapCompletedConsumerBackgroundService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitMqConnectionString = _configuration.GetConnectionString("rabbitmq")
            ?? _configuration["ConnectionStrings:rabbitmq"]
            ?? _configuration["RabbitMQ:ConnectionString"];

        if (string.IsNullOrWhiteSpace(rabbitMqConnectionString))
        {
            _logger.LogWarning("Identity bootstrap completion consumer is disabled because RabbitMQ is not configured.");
            return;
        }

        var factory = new ConnectionFactory
        {
            Uri = new Uri(rabbitMqConnectionString)
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await RabbitMqBoundedRetryTopologyConfigurator.DeclareAsync(channel, RetryTopology, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var integrationEvent = JsonSerializer.Deserialize<UserProfileBootstrapCompletedIntegrationEvent>(eventArgs.Body.ToArray())
                    ?? throw new InvalidOperationException("The UserProfile bootstrap completion payload could not be deserialized.");

                using var scope = _serviceScopeFactory.CreateScope();
                var completionService = scope.ServiceProvider.GetRequiredService<CompleteUserProfileBootstrapService>();
                _ = await completionService.CompleteAsync(integrationEvent, stoppingToken);

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
                    "Identity bootstrap completion consumer dead-lettered delivery tag {DeliveryTag} after {RetryCount} retries.",
                    eventArgs.DeliveryTag,
                    retryCount);

                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
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
                "Identity bootstrap completion consumer scheduled retry {RetryCount}/{MaxRetryAttempts} for delivery tag {DeliveryTag}.",
                nextRetryCount,
                MaxRetryAttempts,
                eventArgs.DeliveryTag);

            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
        catch (Exception retryException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                retryException,
                "Identity bootstrap completion consumer failed to apply bounded retry handling for delivery tag {DeliveryTag}; requeueing original delivery.",
                eventArgs.DeliveryTag);

            await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken: cancellationToken);
        }
    }
}
