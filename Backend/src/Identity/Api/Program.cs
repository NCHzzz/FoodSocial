using FoodSocial.Identity.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

var audience = builder.Configuration["Keycloak:Audience"];
var realm = builder.Configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Keycloak realm is not configured.");
var keycloakAuthority = ResolveKeycloakAuthority(builder.Configuration, realm);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddInfrastructure(builder.Configuration);

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
