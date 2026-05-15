using FoodSocial.Dish.Api.DependencyInjection;
using FoodSocial.Dish.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDishApiServices(builder.Configuration);

var app = builder.Build();

if (DishDatabaseStartup.ShouldApplyMigrationsOnStartup(builder.Configuration, builder.Environment, requireExplicitNonDevelopmentFlag: true))
{
    await DishDatabaseStartup.ApplyDatabaseMigrationsAsync(app.Services);
}

app.ConfigureDishApiPipeline();

app.Run();

public partial class Program;
