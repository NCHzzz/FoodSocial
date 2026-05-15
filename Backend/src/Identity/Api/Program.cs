using FoodSocial.Identity.Api.DependencyInjection;
using FoodSocial.Identity.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddIdentityApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

if (IdentityDatabaseStartup.ShouldApplyMigrationsOnStartup(builder.Configuration, builder.Environment, requireExplicitNonDevelopmentFlag: true))
{
    await IdentityDatabaseStartup.ApplyDatabaseMigrationsAsync(app.Services);
}

app.ConfigureIdentityApiPipeline();

app.Run();

public partial class Program;
