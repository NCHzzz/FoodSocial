using FoodSocial.UserProfile.Api.DependencyInjection;
using FoodSocial.UserProfile.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

Microsoft.Extensions.Hosting.ServiceDefaultsExtensions.AddServiceDefaults(builder);
builder.Services.AddUserProfileApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

if (UserProfileDatabaseStartup.ShouldApplyMigrationsOnStartup(builder.Configuration, builder.Environment, requireExplicitNonDevelopmentFlag: true))
{
    await UserProfileDatabaseStartup.ApplyDatabaseMigrationsAsync(app.Services);
}

app.ConfigureUserProfileApiPipeline();

app.Run();

public partial class Program;
