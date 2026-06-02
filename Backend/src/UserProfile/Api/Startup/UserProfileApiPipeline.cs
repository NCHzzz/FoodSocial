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
                (IConfiguration configuration) =>
                    Results.Ok(new
                    {
                        ApplyMigrationsOnStartup = UserProfileDatabaseStartup.ShouldApplyMigrationsOnStartup(configuration, app.Environment, requireExplicitNonDevelopmentFlag: true),
                        Environment = app.Environment.EnvironmentName
                    })).AllowAnonymous();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        Microsoft.Extensions.Hosting.ServiceDefaultsExtensions.MapDefaultEndpoints(app);

        return app;
    }
}
