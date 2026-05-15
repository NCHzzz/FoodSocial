using FoodSocial.Dish.Domain;
using FoodSocial.Dish.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodSocial.Dish.Api.Tests;

internal sealed class DishApiFactory : WebApplicationFactory<Program>
{
    internal const string ManagementApiKey = "test-dish-management-key";

    private static readonly SemaphoreSlim DatabaseInitializationLock = new(1, 1);
    private static readonly SemaphoreSlim DatabaseAccessLock = new(1, 1);
    private readonly string _connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _connection;
    private bool _databaseInitialized;

    public DishApiFactory()
    {
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:dishdb", "Host=localhost;Database=dish_api_tests;Username=test;Password=test");
        builder.UseSetting("Dish:ManagementApiKey", ManagementApiKey);
        builder.UseSetting("Dish:ApplyMigrationsOnStartup", bool.FalseString);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<DishDbContext>>();
            services.RemoveAll<DbContextOptions<DishDbContext>>();
            services.RemoveAll<DishDbContext>();

            services.AddDbContext<DishDbContext>(options => options.UseSqlite(_connectionString));
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }

    internal async Task<Guid> SeedDishAsync(string name, string? category = null, string? cuisineType = null, bool deprecated = false)
    {
        await EnsureDatabaseCreatedAsync();

        await DatabaseAccessLock.WaitAsync();
        try
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DishDbContext>();
            var dish = DishRecord.Create(name, null, category, cuisineType, [], [], []);
            if (deprecated)
            {
                dish.Deprecate();
            }

            dbContext.Dishes.Add(dish);
            await dbContext.SaveChangesAsync();
            return dish.Id;
        }
        finally
        {
            DatabaseAccessLock.Release();
        }
    }

    internal async Task<Guid> SeedDishInTenantAsync(string name, Guid tenantId)
    {
        await EnsureDatabaseCreatedAsync();

        await DatabaseAccessLock.WaitAsync();
        try
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DishDbContext>();
            var dish = DishRecord.Create(name, null, "Noodles", "Vietnamese", [], [], []);
            dbContext.Dishes.Add(dish);
            dbContext.Entry(dish).Property(nameof(DishRecord.TenantId)).CurrentValue = tenantId;
            await dbContext.SaveChangesAsync();
            return dish.Id;
        }
        finally
        {
            DatabaseAccessLock.Release();
        }
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
            var dbContext = scope.ServiceProvider.GetRequiredService<DishDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
            _databaseInitialized = true;
        }
        finally
        {
            DatabaseInitializationLock.Release();
        }
    }
}
