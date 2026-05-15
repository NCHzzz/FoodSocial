using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class ServiceDefaultsExtensions
{
    private const string LivenessHealthTag = "live";
    private const string HealthEndpointPath = "/health";
    private const string LivenessEndpointPath = "/alive";
    private const string ExposeHealthEndpointsConfigurationKey = "ServiceDefaults:ExposeHealthEndpoints";
    private static readonly TimeSpan HttpAttemptTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan HttpTotalRequestTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan HttpCircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan HttpCircuitBreakerBreakDuration = TimeSpan.FromSeconds(15);

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        ServiceDefaultsExtensions.ConfigureOpenTelemetry(builder);

        ServiceDefaultsExtensions.AddDefaultHealthChecks(builder);

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler(options => ConfigureStandardHttpResilience(options));
            http.AddServiceDiscovery();
        });

        return builder;
    }

    private static void ConfigureStandardHttpResilience(HttpStandardResilienceOptions options)
    {
        options.AttemptTimeout.Timeout = HttpAttemptTimeout;
        options.TotalRequestTimeout.Timeout = HttpTotalRequestTimeout;
        options.Retry.MaxRetryAttempts = 2;
        options.CircuitBreaker.SamplingDuration = HttpCircuitBreakerSamplingDuration;
        options.CircuitBreaker.BreakDuration = HttpCircuitBreakerBreakDuration;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("FoodSocial.Application");
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddSource("Yarp.ReverseProxy")
                    .AddAspNetCoreInstrumentation(tracing =>
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(LivenessEndpointPath)
                    )
                    .AddHttpClientInstrumentation();
            });

        ServiceDefaultsExtensions.AddOpenTelemetryExporters(builder);

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [LivenessHealthTag]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>(ExposeHealthEndpointsConfigurationKey))
        {
            app.MapHealthChecks(HealthEndpointPath).AllowAnonymous();

            app.MapHealthChecks(LivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains(LivenessHealthTag)
            }).AllowAnonymous();
        }

        return app;
    }
}
