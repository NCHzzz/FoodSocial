using System.Security.Claims;
using FoodSocial.Contracts.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);
var audience = builder.Configuration["Keycloak:Audience"] ?? throw new InvalidOperationException("Keycloak audience is not configured.");
var realm = builder.Configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Keycloak realm is not configured.");
var bffClientId = builder.Configuration["Keycloak:BffClientId"] ?? throw new InvalidOperationException("Keycloak BFF client id is not configured.");
var bffClientSecret = builder.Configuration["Keycloak:BffClientSecret"] ?? throw new InvalidOperationException("Keycloak BFF client secret is not configured.");
var keycloakAuthority = ResolveKeycloakAuthority(builder.Configuration, realm);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "FoodSocial.Bff.Auth";
        options.SlidingExpiration = true;
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = keycloakAuthority;
        options.MetadataAddress = $"{keycloakAuthority.TrimEnd('/')}/.well-known/openid-configuration";
        options.ClientId = bffClientId;
        options.ClientSecret = bffClientSecret;
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "preferred_username";
        options.TokenValidationParameters.RoleClaimType = "roles";
        options.ClaimActions.MapUniqueJsonKey("sub", "sub");
        options.ClaimActions.MapUniqueJsonKey("preferred_username", "preferred_username");
        options.ClaimActions.MapUniqueJsonKey("email", "email");
        options.ClaimActions.MapUniqueJsonKey("name", "name");
        options.Scope.Clear();
        options.Scope.Add(OpenIdConnectScope.OpenIdProfile);
        options.Scope.Add("profile");
        options.Scope.Add("email");
    });

builder.Services
    .AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakAuthority;
        options.MetadataAddress = $"{keycloakAuthority.TrimEnd('/')}/.well-known/openid-configuration";
        options.Audience = audience;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapGet("/api/auth/login", async (HttpContext httpContext, string? returnUrl) =>
    {
        var redirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/api/auth/session" : returnUrl;

        await httpContext.ChallengeAsync(
            OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = redirectUri });
    })
    .AllowAnonymous();

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignOutAsync(
            OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = "/" });

        return Results.NoContent();
    })
    .RequireAuthorization();

app.MapGet("/api/auth/session", (ClaimsPrincipal user) =>
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var roles = user.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var response = new CurrentUserResponse(
            new AuthenticatedUserDto(
                user.FindFirstValue("sub") ?? string.Empty,
                user.FindFirstValue("preferred_username") ?? user.Identity?.Name,
                user.FindFirstValue(ClaimTypes.Email),
                roles),
            DateTimeOffset.UtcNow);

        return Results.Ok(response);
    })
    .RequireAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();

static string ResolveKeycloakAuthority(IConfiguration configuration, string realm)
{
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
