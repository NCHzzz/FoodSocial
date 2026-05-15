using FoodSocial.Place.Domain;
using FoodSocial.Place.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodSocial.Place.Api.Tests;

internal sealed class PlaceApiFactory : WebApplicationFactory<Program>
{
    internal const string ManagementApiKey = "test-place-management-key";

    private static readonly SemaphoreSlim DatabaseInitializationLock = new(1, 1);
    private readonly string _connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _connection;
    private bool _databaseInitialized;

    public PlaceApiFactory()
    {
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:placedb", "Host=localhost;Database=place_api_tests;Username=test;Password=test");
        builder.UseSetting("Place:ManagementApiKey", ManagementApiKey);
        builder.UseSetting("Place:ApplyMigrationsOnStartup", bool.FalseString);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<PlaceDbContext>>();
            services.RemoveAll<DbContextOptions<PlaceDbContext>>();
            services.RemoveAll<PlaceDbContext>();

            services.AddDbContext<PlaceDbContext>(options => options.UseSqlite(_connectionString));
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }

    internal async Task<Guid> SeedPlaceAsync(
        string name,
        string addressText,
        string area,
        string city,
        string cuisineType,
        string priceRange,
        string status = "Active")
    {
        await EnsureDatabaseCreatedAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlaceDbContext>();
        var place = PlaceRecord.Create(name, null, addressText, area, city, "Vietnam", null, null, [cuisineType], priceRange);
        if (Enum.TryParse<PlaceLifecycleStatus>(status, ignoreCase: true, out var lifecycleStatus))
        {
            place.ChangeStatus(lifecycleStatus);
        }

        dbContext.Places.Add(place);
        await dbContext.SaveChangesAsync();
        return place.Id;
    }

    internal async Task<Guid> SeedPlaceInTenantAsync(string name, Guid tenantId)
    {
        await EnsureDatabaseCreatedAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlaceDbContext>();
        var place = PlaceRecord.Create(name, null, "2 Foreign Street", "District 1", "Ho Chi Minh City", "Vietnam", null, null, ["Vietnamese"], "$");
        dbContext.Places.Add(place);
        dbContext.Entry(place).Property(nameof(PlaceRecord.TenantId)).CurrentValue = tenantId;
        await dbContext.SaveChangesAsync();
        return place.Id;
    }

    internal async Task EnsureDatabaseCreatedAsync()
    {
        if (_databaseInitialized)
        {
            return;
        }

        await DatabaseInitializationLock.WaitAsync();
        try
        {
            if (_databaseInitialized)
            {
                return;
            }

            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlaceDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
            _databaseInitialized = true;
        }
        finally
        {
            DatabaseInitializationLock.Release();
        }
    }
}
