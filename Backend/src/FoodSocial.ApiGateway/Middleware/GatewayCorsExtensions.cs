namespace FoodSocial.ApiGateway;

internal static class GatewayCorsExtensions
{
    public const string PolicyName = "FoodSocialGatewayCors";

    public static IServiceCollection AddGatewayCors(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var allowedOrigins = configuration
            .GetSection("Gateway:Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length == 0 && !environment.IsDevelopment())
        {
            throw new InvalidOperationException("Gateway CORS allowed origins must be configured outside Development.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (allowedOrigins.Length == 0)
                {
                    policy.AllowAnyHeader().AllowAnyMethod();
                    return;
                }

                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
