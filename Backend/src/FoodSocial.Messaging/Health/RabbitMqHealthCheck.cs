using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace FoodSocial.Messaging;

/// <summary>
/// Verifies that a service can open a RabbitMQ connection and channel for readiness checks.
/// </summary>
public sealed class RabbitMqHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("rabbitmq")
            ?? configuration["ConnectionStrings:rabbitmq"]
            ?? configuration["RabbitMQ:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("RabbitMQ connection string is not configured.");
        }

        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString)
        };

        try
        {
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            return channel.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ channel opened successfully.")
                : HealthCheckResult.Unhealthy("RabbitMQ channel could not be opened.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ readiness check failed.", exception);
        }
    }
}
