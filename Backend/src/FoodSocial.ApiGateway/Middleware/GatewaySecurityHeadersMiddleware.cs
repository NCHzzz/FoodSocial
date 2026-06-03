namespace FoodSocial.ApiGateway;

internal static class GatewaySecurityHeadersMiddleware
{
    public static IApplicationBuilder UseGatewaySecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["Referrer-Policy"] = "no-referrer";
                headers["X-Frame-Options"] = "DENY";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["Cross-Origin-Resource-Policy"] = "same-site";
                headers["X-Permitted-Cross-Domain-Policies"] = "none";
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
                headers["X-FoodSocial-Edge"] = "api-gateway";
                return Task.CompletedTask;
            });

            await next(context);
        });
    }
}
