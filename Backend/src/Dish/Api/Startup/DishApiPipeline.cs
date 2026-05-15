namespace FoodSocial.Dish.Api.Startup;

/// <summary>
/// Configures Dish API middleware and endpoints.
/// </summary>
public static class DishApiPipeline
{
    /// <summary>
    /// Adds Dish API middleware and maps endpoints.
    /// </summary>
    public static WebApplication ConfigureDishApiPipeline(this WebApplication app)
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
