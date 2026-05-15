using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodSocial.ApiGateway;

internal sealed class GatewayBffHealthCheck(HttpClient httpClient) : IHealthCheck
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("BFF readiness endpoint is reachable through the gateway dependency check.")
                : HealthCheckResult.Unhealthy($"BFF readiness endpoint returned {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("BFF readiness endpoint is unreachable.", exception);
        }
    }
}
