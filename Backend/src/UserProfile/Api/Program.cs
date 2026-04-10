using FoodSocial.UserProfile.Application;
using FoodSocial.UserProfile.Infrastructure;
using FoodSocial.UserProfile.Infrastructure.Messaging;
using FoodSocial.UserProfile.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<UserProvisionedConsumerBackgroundService>();

var app = builder.Build();

if (builder.Configuration.GetValue("UserProfile:ApplyMigrationsOnStartup", true))
{
    await EnsureDatabaseExistsAsync(app.Services, "userprofiledb");
    await ApplyUserProfileDatabaseMigrationsAsync(app.Services);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();

static async Task ApplyUserProfileDatabaseMigrationsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
    await dbContext.Database.MigrateAsync();
}

static async Task EnsureDatabaseExistsAsync(IServiceProvider services, string databaseName)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
    var connectionString = dbContext.Database.GetConnectionString()
        ?? throw new InvalidOperationException("UserProfile database connection string is not configured.");

    var adminConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Database = "postgres"
    }.ConnectionString;

    await using var connection = new NpgsqlConnection(adminConnectionString);
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = $"CREATE DATABASE \"{databaseName.Replace("\"", "\"\"", StringComparison.Ordinal)}\";";

    try
    {
        await command.ExecuteNonQueryAsync();
    }
    catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.DuplicateDatabase)
    {
    }
}
