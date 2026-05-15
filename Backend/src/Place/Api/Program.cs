using FoodSocial.Place.Api.DependencyInjection;
using FoodSocial.Place.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddPlaceApiServices(builder.Configuration);

var app = builder.Build();

if (PlaceDatabaseStartup.ShouldApplyMigrationsOnStartup(builder.Configuration, builder.Environment, requireExplicitNonDevelopmentFlag: true))
{
    await PlaceDatabaseStartup.ApplyDatabaseMigrationsAsync(app.Services);
}

app.ConfigurePlaceApiPipeline();
app.Run();

public partial class Program;
