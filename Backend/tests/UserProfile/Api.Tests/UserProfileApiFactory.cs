using FoodSocial.UserProfile.Domain;
using FoodSocial.UserProfile.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace FoodSocial.UserProfile.Api.Tests;

internal sealed class UserProfileApiFactory : WebApplicationFactory<Program>
{
    private static readonly SemaphoreSlim DatabaseInitializationLock = new(1, 1);
    private readonly string _connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _connection;
    private bool _databaseInitialized;

    public UserProfileApiFactory()
    {
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Keycloak:Realm", "foodsocial");
        builder.UseSetting("Keycloak:Authority", "https://identity.example.test/realms/foodsocial");
        builder.UseSetting("ConnectionStrings:userprofiledb", "Host=localhost;Database=userprofile_api_tests;Username=test;Password=test");
        builder.UseSetting("UserProfile:ApplyMigrationsOnStartup", bool.FalseString);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<UserProfileDbContext>>();
            services.RemoveAll<DbContextOptions<UserProfileDbContext>>();
            services.RemoveAll<UserProfileDbContext>();

            services.AddDbContext<UserProfileDbContext>(options => options.UseSqlite(_connectionString));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();

        await base.DisposeAsync();
    }

    internal async Task<Guid> SeedProfileAsync(
        Guid identityId,
        string subject,
        string? displayName = null,
        string? bio = null,
        string? avatarUrl = null,
        IReadOnlyList<string>? preferredCuisines = null,
        IReadOnlyList<string>? dislikedIngredients = null,
        IReadOnlyList<string>? dietaryRestrictions = null)
    {
        await EnsureDatabaseCreatedAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();

        var shell = new UserProfileShell(identityId, subject);
        var detail = shell.EnsureDetail(displayName);
        detail.Update(displayName, avatarUrl, bio, preferredCuisines, dislikedIngredients, dietaryRestrictions);

        dbContext.UserProfileShells.Add(shell);
        await dbContext.SaveChangesAsync();
        return shell.Id;
    }

    internal async Task SeedFollowAsync(Guid followerShellId, Guid followedShellId)
    {
        await EnsureDatabaseCreatedAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
        dbContext.UserFollows.Add(new UserFollow(followerShellId, followedShellId));
        await dbContext.SaveChangesAsync();
    }

    internal async Task SeedSavedPostAsync(Guid shellId, Guid postId)
    {
        await EnsureDatabaseCreatedAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
        dbContext.SavedPosts.Add(new SavedPost(shellId, postId));
        await dbContext.SaveChangesAsync();
    }

    internal async Task SeedSavedDishAsync(Guid shellId, Guid dishId)
    {
        await EnsureDatabaseCreatedAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
        dbContext.SavedDishes.Add(new SavedDish(shellId, dishId));
        await dbContext.SaveChangesAsync();
    }

    internal async Task SeedSavedPlaceAsync(Guid shellId, Guid placeId)
    {
        await EnsureDatabaseCreatedAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
        dbContext.SavedPlaces.Add(new SavedPlace(shellId, placeId));
        await dbContext.SaveChangesAsync();
    }

    private async Task EnsureDatabaseCreatedAsync()
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
            var dbContext = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
            _databaseInitialized = true;
        }
        finally
        {
            DatabaseInitializationLock.Release();
        }
    }
}
