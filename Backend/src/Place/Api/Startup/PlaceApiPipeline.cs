namespace FoodSocial.Place.Api.Startup;

/// <summary>
/// Configures Place API middleware and endpoints.
/// </summary>
public static class PlaceApiPipeline
{
    /// <summary>
    /// Adds Place API middleware and maps endpoints.
    /// </summary>
    public static WebApplication ConfigurePlaceApiPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.MapDefaultEndpoints();

        return app;
    }
}
