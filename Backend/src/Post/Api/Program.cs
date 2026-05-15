using FoodSocial.Post.Api.DependencyInjection;
using FoodSocial.Post.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddPostApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

if (PostDatabaseStartup.ShouldApplyMigrationsOnStartup(builder.Configuration, builder.Environment, requireExplicitNonDevelopmentFlag: true))
{
    await PostDatabaseStartup.ApplyDatabaseMigrationsAsync(app.Services);
}

app.ConfigurePostApiPipeline();

app.Run();

public partial class Program;
