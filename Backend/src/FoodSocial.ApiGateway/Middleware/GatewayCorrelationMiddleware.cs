namespace FoodSocial.ApiGateway;

internal static class GatewayCorrelationMiddleware
{
    private const string CorrelationHeaderName = "X-Correlation-ID";
    private const int MaxCorrelationIdLength = 128;

    public static IApplicationBuilder UseGatewayCorrelation(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            var correlationId = ResolveCorrelationId(context);
            context.Request.Headers[CorrelationHeaderName] = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationHeaderName] = correlationId;
                return Task.CompletedTask;
            });

            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("FoodSocial.ApiGateway.Correlation");
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                await next(context);
            }
        });
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationHeaderName, out var headerValues)
            && headerValues.Count == 1)
        {
            var requestedCorrelationId = headerValues.ToString().Trim();
            if (IsSafeCorrelationId(requestedCorrelationId))
            {
                return requestedCorrelationId;
            }
        }

        return context.TraceIdentifier;
    }

    private static bool IsSafeCorrelationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxCorrelationIdLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (character < 0x21 || character > 0x7E)
            {
                return false;
            }
        }

        return true;
    }
}
