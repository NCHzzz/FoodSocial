namespace FoodSocial.Identity.Api.DependencyInjection;

/// <summary>
/// Resolves Identity API Keycloak authentication settings.
/// </summary>
public static class KeycloakStartup
{
    /// <summary>
    /// Resolves Keycloak authority from explicit configuration or Aspire service discovery.
    /// </summary>
    public static string ResolveKeycloakAuthority(IConfiguration configuration, string realm)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredAuthority = configuration["Keycloak:Authority"];

        if (!string.IsNullOrWhiteSpace(configuredAuthority))
        {
            return configuredAuthority;
        }

        var discoveredAuthority = configuration["Services:keycloak:http:0"]
            ?? configuration["Services:keycloak:https:0"]
            ?? throw new InvalidOperationException(
                "Keycloak authority is not configured. Set Keycloak:Authority or provide an Aspire service reference for 'keycloak'.");

        return $"{discoveredAuthority.TrimEnd('/')}/realms/{realm}";
    }

    /// <summary>
    /// Resolves whether JWT metadata must use HTTPS.
    /// </summary>
    public static bool ResolveRequireHttpsMetadata(IConfiguration configuration, IHostEnvironment environment, string keycloakAuthority)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var configuredRequirement = configuration.GetValue<bool?>("Authentication:RequireHttpsMetadata")
            ?? !environment.IsDevelopment();

        if (!Uri.TryCreate(keycloakAuthority, UriKind.Absolute, out var authorityUri))
        {
            return configuredRequirement;
        }

        return configuredRequirement && authorityUri.Scheme == Uri.UriSchemeHttps;
    }

    /// <summary>
    /// Determines whether local development may use an insecure Keycloak backchannel.
    /// </summary>
    public static bool ShouldAllowInsecureDevBackchannel(IHostEnvironment environment, string keycloakAuthority)
    {
        ArgumentNullException.ThrowIfNull(environment);

        if (!environment.IsDevelopment())
        {
            return false;
        }

        return Uri.TryCreate(keycloakAuthority, UriKind.Absolute, out var authorityUri)
            && authorityUri.Scheme == Uri.UriSchemeHttps
            && string.Equals(authorityUri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates development-only JWT backchannel handler.
    /// </summary>
    public static HttpMessageHandler CreateDevelopmentBackchannelHandler() =>
        new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
}
