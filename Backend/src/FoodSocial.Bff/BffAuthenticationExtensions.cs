using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace FoodSocial.Bff;

internal static class BffAuthenticationExtensions
{
    public static IServiceCollection AddBffIdentityOrchestrationClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddHttpClient<IdentityOrchestrationHttpClient>((serviceProvider, client) =>
            {
                client.BaseAddress = ResolveIdentityApiBaseAddress(serviceProvider.GetRequiredService<IConfiguration>());
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                var identityApiBaseAddress = ResolveIdentityApiBaseAddress(configuration);

                return ShouldAllowInsecureDevServiceBackchannel(environment, identityApiBaseAddress)
                    ? CreateDevelopmentBackchannelHandler()
                    : new HttpClientHandler();
            });

        return services;
    }

    public static IServiceCollection AddBffUserProfileClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddHttpClient<UserProfileHttpClient>((serviceProvider, client) =>
            {
                client.BaseAddress = ResolveUserProfileApiBaseAddress(serviceProvider.GetRequiredService<IConfiguration>());
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                var userProfileApiBaseAddress = ResolveUserProfileApiBaseAddress(configuration);

                return ShouldAllowInsecureDevServiceBackchannel(environment, userProfileApiBaseAddress)
                    ? CreateDevelopmentBackchannelHandler()
                    : new HttpClientHandler();
            });

        return services;
    }

    public static IServiceCollection AddBffDishClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddHttpClient<DishHttpClient>((serviceProvider, client) =>
            {
                client.BaseAddress = ResolveDishApiBaseAddress(serviceProvider.GetRequiredService<IConfiguration>());
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                var dishApiBaseAddress = ResolveDishApiBaseAddress(configuration);

                return ShouldAllowInsecureDevServiceBackchannel(environment, dishApiBaseAddress)
                    ? CreateDevelopmentBackchannelHandler()
                    : new HttpClientHandler();
            });

        return services;
    }

    public static IServiceCollection AddBffPlaceClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddHttpClient<PlaceHttpClient>((serviceProvider, client) =>
            {
                client.BaseAddress = ResolvePlaceApiBaseAddress(serviceProvider.GetRequiredService<IConfiguration>());
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                var placeApiBaseAddress = ResolvePlaceApiBaseAddress(configuration);

                return ShouldAllowInsecureDevServiceBackchannel(environment, placeApiBaseAddress)
                    ? CreateDevelopmentBackchannelHandler()
                    : new HttpClientHandler();
            });

        return services;
    }

    public static AuthenticationBuilder AddBffAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var audience = configuration["Keycloak:Audience"] ?? throw new InvalidOperationException("Keycloak audience is not configured.");
        var realm = configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Keycloak realm is not configured.");
        var keycloakAuthority = ResolveKeycloakAuthority(configuration, realm);
        var requireHttpsMetadata = ResolveRequireHttpsMetadata(configuration, environment, keycloakAuthority);
        var allowInsecureDevBackchannel = ShouldAllowInsecureDevBackchannel(environment, keycloakAuthority);
        var bffClientId = configuration["Keycloak:BffClientId"] ?? throw new InvalidOperationException("Keycloak BFF client id is not configured.");
        var bffClientSecret = configuration["Keycloak:BffClientSecret"] ?? throw new InvalidOperationException("Keycloak BFF client secret is not configured.");

        return services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = BffAuthenticationDefaults.Scheme;
                options.DefaultAuthenticateScheme = BffAuthenticationDefaults.Scheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddPolicyScheme(BffAuthenticationDefaults.Scheme, BffAuthenticationDefaults.Scheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                    BffRequestConventions.HasBearerAuthorizationHeader(context.Request)
                        ? JwtBearerDefaults.AuthenticationScheme
                        : CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "__Host-FoodSocial.Bff";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = keycloakAuthority;
                options.MetadataAddress = $"{keycloakAuthority.TrimEnd('/')}/.well-known/openid-configuration";
                options.ClientId = bffClientId;
                options.ClientSecret = bffClientSecret;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.MapInboundClaims = false;
                options.SaveTokens = true;
                options.CallbackPath = "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";
                if (allowInsecureDevBackchannel)
                {
                    options.BackchannelHttpHandler = CreateDevelopmentBackchannelHandler();
                }

                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    var redirectUri = context.Properties.RedirectUri;

                    if (!string.IsNullOrWhiteSpace(redirectUri) && !Uri.TryCreate(redirectUri, UriKind.Relative, out _))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }

                    context.ProtocolMessage.RedirectUri = $"{ResolveBffPublicOrigin(context.Request, configuration)}{options.CallbackPath}";

                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToIdentityProviderForSignOut = context =>
                {
                    context.ProtocolMessage.PostLogoutRedirectUri = $"{ResolveBffPublicOrigin(context.Request, configuration)}{options.SignedOutCallbackPath}";
                    return Task.CompletedTask;
                };

                options.Scope.Clear();
                options.Scope.Add(OpenIdConnectScope.OpenId);
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = keycloakAuthority;
                options.MetadataAddress = $"{keycloakAuthority.TrimEnd('/')}/.well-known/openid-configuration";
                options.Audience = audience;
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.MapInboundClaims = false;
                if (allowInsecureDevBackchannel)
                {
                    options.BackchannelHttpHandler = CreateDevelopmentBackchannelHandler();
                }
            });
    }

    private static string ResolveBffPublicOrigin(HttpRequest request, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredOrigin = configuration["Bff:PublicOrigin"];
        if (!string.IsNullOrWhiteSpace(configuredOrigin))
        {
            return configuredOrigin.TrimEnd('/');
        }

        return $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
    }

    private static Uri ResolveIdentityApiBaseAddress(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var discoveredAddress = configuration["Services:identity-api:https:0"]
            ?? configuration["Services:identity-api:http:0"]
            ?? throw new InvalidOperationException(
                "Identity API base address is not configured. Set an Aspire service reference for 'identity-api'.");

        return new Uri(discoveredAddress.TrimEnd('/'), UriKind.Absolute);
    }

    private static Uri ResolveUserProfileApiBaseAddress(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var discoveredAddress = configuration["Services:userprofile-api:https:0"]
            ?? configuration["Services:userprofile-api:http:0"]
            ?? throw new InvalidOperationException(
                "UserProfile API base address is not configured. Set an Aspire service reference for 'userprofile-api'.");

        return new Uri(discoveredAddress.TrimEnd('/'), UriKind.Absolute);
    }

    private static Uri ResolveDishApiBaseAddress(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var discoveredAddress = configuration["Services:dish-api:https:0"]
            ?? configuration["Services:dish-api:http:0"]
            ?? throw new InvalidOperationException(
                "Dish API base address is not configured. Set an Aspire service reference for 'dish-api'.");

        return new Uri(discoveredAddress.TrimEnd('/'), UriKind.Absolute);
    }

    private static Uri ResolvePlaceApiBaseAddress(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var discoveredAddress = configuration["Services:place-api:https:0"]
            ?? configuration["Services:place-api:http:0"]
            ?? throw new InvalidOperationException(
                "Place API base address is not configured. Set an Aspire service reference for 'place-api'.");

        return new Uri(discoveredAddress.TrimEnd('/'), UriKind.Absolute);
    }

    private static string ResolveKeycloakAuthority(IConfiguration configuration, string realm)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(realm);

        var configuredAuthority = configuration["Keycloak:Authority"];

        if (!string.IsNullOrWhiteSpace(configuredAuthority))
        {
            return configuredAuthority;
        }

        var discoveredAuthority = configuration["Services:keycloak:https:0"]
            ?? configuration["Services:keycloak:http:0"]
            ?? throw new InvalidOperationException(
                "Keycloak authority is not configured. Set Keycloak:Authority or provide an Aspire service reference for 'keycloak'.");

        return $"{discoveredAuthority.TrimEnd('/')}/realms/{realm}";
    }

    private static bool ResolveRequireHttpsMetadata(IConfiguration configuration, IHostEnvironment environment, string keycloakAuthority)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(keycloakAuthority);

        var configuredRequirement = configuration.GetValue<bool?>("Authentication:RequireHttpsMetadata")
            ?? !environment.IsDevelopment();

        if (!Uri.TryCreate(keycloakAuthority, UriKind.Absolute, out var authorityUri))
        {
            return configuredRequirement;
        }

        return configuredRequirement && authorityUri.Scheme == Uri.UriSchemeHttps;
    }

    private static bool ShouldAllowInsecureDevBackchannel(IHostEnvironment environment, string keycloakAuthority)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(keycloakAuthority);

        if (!environment.IsDevelopment())
        {
            return false;
        }

        return Uri.TryCreate(keycloakAuthority, UriKind.Absolute, out var authorityUri)
            && authorityUri.Scheme == Uri.UriSchemeHttps
            && string.Equals(authorityUri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldAllowInsecureDevServiceBackchannel(IHostEnvironment environment, Uri serviceBaseAddress)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(serviceBaseAddress);

        return environment.IsDevelopment()
            && serviceBaseAddress.Scheme == Uri.UriSchemeHttps
            && string.Equals(serviceBaseAddress.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static HttpMessageHandler CreateDevelopmentBackchannelHandler() =>
        new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
}
