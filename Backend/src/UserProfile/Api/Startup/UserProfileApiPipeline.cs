using FoodSocial.UserProfile.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodSocial.UserProfile.Api.Startup;

/// <summary>
/// Configures UserProfile API middleware and endpoints.
/// </summary>
public static class UserProfileApiPipeline
{
    /// <summary>
    /// Adds UserProfile API middleware and maps endpoints.
    /// </summary>
    public static WebApplication ConfigureUserProfileApiPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
            app.MapGet(
                "/_diagnostics/userprofile-startup",
                (IConfiguration configuration, UserProfileDbContext dbContext) =>
                {
                    var connectionString = dbContext.Database.GetConnectionString();
                    var connectionBuilder = string.IsNullOrWhiteSpace(connectionString)
                        ? null
                        : new NpgsqlConnectionStringBuilder(connectionString);

                    return Results.Ok(new
                    {
                        ApplyMigrationsOnStartup = UserProfileDatabaseStartup.ShouldApplyMigrationsOnStartup(configuration, app.Environment, requireExplicitNonDevelopmentFlag: true),
                        Provider = dbContext.Database.ProviderName,
                        Database = connectionBuilder?.Database,
                        Host = connectionBuilder?.Host,
                        Port = connectionBuilder?.Port
                    });
                }).AllowAnonymous();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        Microsoft.Extensions.Hosting.ServiceDefaultsExtensions.MapDefaultEndpoints(app);

        return app;
    }
}
