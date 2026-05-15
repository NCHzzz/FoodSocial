namespace FoodSocial.Post.Api.Startup;

/// <summary>
/// Configures Post API middleware and endpoints.
/// </summary>
public static class PostApiPipeline
{
    /// <summary>
    /// Adds Post API middleware and maps endpoints.
    /// </summary>
    public static WebApplication ConfigurePostApiPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

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

        return app;
    }
}
