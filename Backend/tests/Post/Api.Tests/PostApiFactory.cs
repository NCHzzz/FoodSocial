using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application;
using FoodSocial.Post.Api;
using FoodSocial.Post.Infrastructure.Persistence;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodSocial.Post.Api.Tests;

internal sealed class PostApiFactory : WebApplicationFactory<Program>
{
    internal const string TestInternalSecret = "post-test-internal-secret";
    internal static readonly Guid TestCallerIdentityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TestCatalogReferenceValidator _catalogReferenceValidator = new();
    private readonly string _mediaStoragePath = Path.Combine(Path.GetTempPath(), "foodsocial-post-api-tests", Guid.NewGuid().ToString("N"));
    private bool _databaseInitialized;

    public PostApiFactory() => _connection.Open();
    internal string MediaStoragePath => _mediaStoragePath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:postdb", "Host=localhost;Database=post_api_tests;Username=test;Password=test");
        builder.UseSetting("Post:ApplyMigrationsOnStartup", bool.FalseString);
        builder.UseSetting("Post:InternalTrust:SharedSecret", TestInternalSecret);
        builder.UseSetting("Post:Media:StoragePath", _mediaStoragePath);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<PostDbContext>>();
            services.RemoveAll<DbContextOptions<PostDbContext>>();
            services.RemoveAll<PostDbContext>();
            services.RemoveAll<IPostCatalogReferenceValidator>();
            services.AddSingleton<IPostCatalogReferenceValidator>(_catalogReferenceValidator);
            services.AddDbContext<PostDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        if (Directory.Exists(_mediaStoragePath))
        {
            Directory.Delete(_mediaStoragePath, recursive: true);
        }

        await base.DisposeAsync();
    }

    internal HttpClient CreateTrustedClient(Guid? identityId = null, bool moderator = false)
    {
        var client = CreateClient();
        AddTrustHeaders(client, identityId ?? TestCallerIdentityId, moderator);
        return client;
    }

    internal static void AddTrustHeaders(HttpClient client, Guid identityId, bool moderator = false)
    {
        client.DefaultRequestHeaders.Add("X-FoodSocial-Internal-Secret", TestInternalSecret);
        client.DefaultRequestHeaders.Add("X-FoodSocial-Identity-Id", identityId.ToString());
        if (moderator)
        {
            client.DefaultRequestHeaders.Add("X-FoodSocial-Identity-Roles", "moderator");
        }
    }

    internal void RegisterActiveDish(Guid dishId) => _catalogReferenceValidator.RegisterDish(dishId, "Active");
    internal void RegisterDeprecatedDish(Guid dishId) => _catalogReferenceValidator.RegisterDish(dishId, "Deprecated");
    internal void RegisterActivePlace(Guid placeId) => _catalogReferenceValidator.RegisterPlace(placeId, "Active");
    internal void RegisterClosedPlace(Guid placeId) => _catalogReferenceValidator.RegisterPlace(placeId, "Closed");

    internal async Task EnsureDatabaseCreatedAsync()
    {
        if (_databaseInitialized) return;
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<PostDbContext>().Database.EnsureCreatedAsync();
        _databaseInitialized = true;
    }

    internal Task CloseDatabaseConnectionAsync() => _connection.CloseAsync();

    internal async Task<Guid> CreatePostAsync(HttpClient client, Guid? authorId = null, Guid? dishId = null, string caption = "Great bowl", string tag = "pho")
    {
        await EnsureDatabaseCreatedAsync();
        var resolvedDishId = dishId ?? Guid.NewGuid();
        RegisterActiveDish(resolvedDishId);
        var request = new CreatePostRequest(authorId ?? Guid.NewGuid(), resolvedDishId, caption, [new PostMediaRequest("https://cdn.example/post.jpg", IsPrimary: true)], Tags: [tag], Rating: 5, LocationText: "District 1");
        using var response = await client.PostAsJsonAsync("/api/posts", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PostDetailResponse>())!.Id;
    }

    private sealed class TestCatalogReferenceValidator : IPostCatalogReferenceValidator
    {
        private readonly Dictionary<Guid, string> _dishes = new();
        private readonly Dictionary<Guid, string> _places = new();

        internal void RegisterDish(Guid id, string status) => _dishes[id] = status;
        internal void RegisterPlace(Guid id, string status) => _places[id] = status;

        public Task<PostCatalogReferenceValidationResult> ValidateReferencesAsync(Guid dishId, Guid? placeId, CancellationToken cancellationToken)
        {
            if (!_dishes.TryGetValue(dishId, out var dishStatus))
            {
                return Task.FromResult(PostCatalogReferenceValidationResult.Invalid("Dish must reference an existing canonical dish."));
            }

            if (!string.Equals(dishStatus, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(PostCatalogReferenceValidationResult.Invalid("Dish must reference an active canonical dish."));
            }

            if (placeId is { } place && place != Guid.Empty)
            {
                if (!_places.TryGetValue(place, out var placeStatus))
                {
                    return Task.FromResult(PostCatalogReferenceValidationResult.Invalid("Place must reference an existing canonical place when provided."));
                }

                if (!string.Equals(placeStatus, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(PostCatalogReferenceValidationResult.Invalid("Place must reference an active canonical place when provided."));
                }
            }

            return Task.FromResult(PostCatalogReferenceValidationResult.Valid);
        }
    }
}
