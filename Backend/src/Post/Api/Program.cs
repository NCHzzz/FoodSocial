using FoodSocial.Post.Api;
using FoodSocial.Post.Application;
using FoodSocial.Post.Infrastructure;
using FoodSocial.Post.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddScoped<PostInternalAuthorizationFilter>();
builder.Services.AddScoped<PostModeratorAuthorizationFilter>();
builder.Services.AddSingleton<PostMediaStorage>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck<PostPostgresHealthCheck>("post-postgres");

var app = builder.Build();

var applyMigrationsOnStartup = ShouldApplyMigrationsOnStartup(builder.Configuration, builder.Environment, "Post", requireExplicitNonDevelopmentFlag: true);
if (applyMigrationsOnStartup)
{
    await ApplyPostDatabaseMigrationsAsync(app.Services);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();

static async Task ApplyPostDatabaseMigrationsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PostDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("FoodSocial.Post.Api.DatabaseInitialization");
    var connectionString = dbContext.Database.GetConnectionString() ?? throw new InvalidOperationException("Post database connection string is not configured.");
    var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);
    logger.LogInformation("Applying Post EF migrations against database {DatabaseName} on host {Host}:{Port}.", connectionBuilder.Database, connectionBuilder.Host, connectionBuilder.Port);
    await dbContext.Database.MigrateAsync();
    logger.LogInformation("Finished applying Post EF migrations against database {DatabaseName}.", connectionBuilder.Database);
}

static bool ShouldApplyMigrationsOnStartup(
    IConfiguration configuration,
    IHostEnvironment environment,
    string serviceName,
    bool requireExplicitNonDevelopmentFlag = false)
{
    var configuredValue = configuration.GetValue<bool?>($"{serviceName}:ApplyMigrationsOnStartup");
    if (configuredValue is not null)
    {
        return configuredValue.Value;
    }

    if (!environment.IsDevelopment() && requireExplicitNonDevelopmentFlag)
    {
        throw new InvalidOperationException($"{serviceName}:ApplyMigrationsOnStartup must be explicitly configured outside development.");
    }

    return environment.IsDevelopment();
}

public partial class Program;
