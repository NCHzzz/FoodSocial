using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace FoodSocial.Bff;

internal static class BffRateLimitingExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IServiceCollection AddBffRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<BffRateLimitingOptions>(configuration.GetSection(BffRateLimitingOptions.SectionName));
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var rateLimitOptions = context.RequestServices.GetRequiredService<IOptions<BffRateLimitingOptions>>().Value;
                if (!rateLimitOptions.Enabled || IsBypassPath(context.Request.Path))
                {
                    return RateLimitPartition.GetNoLimiter("bff-bypass");
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
                var logger = httpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("FoodSocial.Bff.RateLimiting");
                var policyName = ResolvePolicy(httpContext.Request, httpContext.RequestServices.GetRequiredService<IOptions<BffRateLimitingOptions>>().Value).Name;

                logger.LogWarning(
                    "BFF rate limit rejected request. Policy: {RateLimitPolicy}. Method: {Method}. Path: {Path}. TraceId: {TraceId}.",
                    policyName,
                    httpContext.Request.Method,
                    httpContext.Request.Path.Value,
                    httpContext.TraceIdentifier);

                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                httpContext.Response.ContentType = "application/problem+json";
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    httpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Type = "https://foodsocial/errors/bff/rate-limit-exceeded",
                    Title = "Rate limit exceeded",
                    Detail = "Too many requests were sent to this FoodSocial endpoint. Please retry later.",
                    Status = StatusCodes.Status429TooManyRequests,
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

    private static BffResolvedRateLimitPolicy ResolvePolicy(HttpRequest request, BffRateLimitingOptions options)
    {
        var path = request.Path;
        var method = request.Method;

        if (path.StartsWithSegments("/api/auth/login")
            || path.StartsWithSegments("/api/auth/refresh")
            || path.StartsWithSegments("/api/auth/logout"))
        {
            return new("auth", options.Auth);
        }

        if (path.StartsWithSegments("/api/media/uploads") && HttpMethods.IsPost(method))
        {
            return new("media-upload", options.MediaUpload);
        }

        if (path.StartsWithSegments("/api/search/assist")
            || path.StartsWithSegments("/api/recommendations")
            || path.StartsWithSegments("/api/posts/search"))
        {
            return new("search-assist", options.SearchAssist);
        }

        if (IsRiskyWrite(request))
        {
            return new("risky-write", options.RiskyWrite);
        }

        return new("default", options.Default);
    }

    private static bool IsRiskyWrite(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method))
        {
            return false;
        }

        var path = request.Path;
        return path.StartsWithSegments("/api/posts")
            || path.StartsWithSegments("/api/comments")
            || path.StartsWithSegments("/api/users")
            || path.Value?.Contains("/report", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsBypassPath(PathString path) =>
        path.StartsWithSegments("/health")
        || path.StartsWithSegments("/alive")
        || path.StartsWithSegments("/openapi");

    private static string ResolveClientKey(HttpContext context)
    {
        var subject = context.User.Identity?.IsAuthenticated == true
            ? FoodSocial.Contracts.Identity.IdentityClaimReader.GetSubject(context.User)
            : null;
        if (!string.IsNullOrWhiteSpace(subject))
        {
            return $"sub:{subject}";
        }

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return $"xff:{forwardedFor.Split(',')[0].Trim()}";
        }

        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    private sealed record BffResolvedRateLimitPolicy(string Name, BffRateLimitPolicyOptions Options);
}
