using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodSocial.ApiGateway;

internal static class GatewayRateLimitingExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<GatewayRateLimitingOptions>(configuration.GetSection(GatewayRateLimitingOptions.SectionName));
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var rateLimitOptions = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<GatewayRateLimitingOptions>>().Value;
                if (!rateLimitOptions.Enabled || IsBypassPath(context.Request.Path))
                {
                    return RateLimitPartition.GetNoLimiter("gateway-bypass");
                }

                var policy = ResolvePolicy(context.Request, rateLimitOptions);
                var clientKey = ResolveClientKey(context);
                var partitionKey = $"{policy.Name}:{clientKey}";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(1, policy.Options.PermitLimit),
                    Window = TimeSpan.FromSeconds(Math.Max(1, policy.Options.WindowSeconds)),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                var policyName = ResolvePolicy(
                    httpContext.Request,
                    httpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<GatewayRateLimitingOptions>>().Value).Name;

                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                httpContext.Response.ContentType = "application/problem+json";
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    httpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                var problem = new ProblemDetails
                {
                    Type = "https://foodsocial/errors/gateway/rate-limit-exceeded",
                    Title = "Rate limit exceeded",
                    Detail = "Too many requests were sent to the FoodSocial API Gateway. Please retry later.",
                    Status = StatusCodes.Status429TooManyRequests,
                    Instance = httpContext.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = httpContext.TraceIdentifier,
                        ["rateLimitPolicy"] = policyName
                    }
                };

                await JsonSerializer.SerializeAsync(httpContext.Response.Body, problem, JsonOptions, cancellationToken);
            };
        });

        return services;
    }

    private static GatewayResolvedRateLimitPolicy ResolvePolicy(HttpRequest request, GatewayRateLimitingOptions options)
    {
        if (request.Path.StartsWithSegments("/api/auth/login")
            || request.Path.StartsWithSegments("/api/auth/refresh")
            || request.Path.StartsWithSegments("/api/auth/logout")
            || request.Path.StartsWithSegments("/signin-oidc")
            || request.Path.StartsWithSegments("/signout-callback-oidc"))
        {
            return new("auth", options.Auth);
        }

        if (request.Path.StartsWithSegments("/api/media/uploads") && HttpMethods.IsPost(request.Method))
        {
            return new("media-upload", options.MediaUpload);
        }

        if (IsRiskyWrite(request))
        {
            return new("risky-write", options.RiskyWrite);
        }

        return new("default", options.Default);
    }

    private static bool IsRiskyWrite(HttpRequest request) =>
        !HttpMethods.IsGet(request.Method)
        && !HttpMethods.IsHead(request.Method)
        && !HttpMethods.IsOptions(request.Method);

    private static bool IsBypassPath(PathString path) =>
        path.StartsWithSegments("/health")
        || path.StartsWithSegments("/alive")
        || path.StartsWithSegments("/openapi");

    private static string ResolveClientKey(HttpContext context)
        => $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

    private sealed record GatewayResolvedRateLimitPolicy(string Name, GatewayRateLimitPolicyOptions Options);
}
