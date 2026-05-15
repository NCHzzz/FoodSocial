namespace FoodSocial.Identity.Api.Startup;

/// <summary>
/// Configures Identity API middleware and endpoints.
/// </summary>
public static class IdentityApiPipeline
{
    /// <summary>
    /// Adds Identity API middleware and maps endpoints.
    /// </summary>
    public static WebApplication ConfigureIdentityApiPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        app.MapDefaultEndpoints();

        return app;
    }
}
