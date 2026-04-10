using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Xunit;

namespace FoodSocial.AppHost.Tests;

public sealed class AppHostFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);
    internal const string TestKeycloakBffClientSecret = "foodsocial-bff-test-secret";
    internal const string TestKeycloakDemoUserPassword = "Pass@word1";
    internal const string TestIdentityInternalTrustSharedSecret = "foodsocial-bff-test-shared-secret";

    private readonly DirectoryInfo _tempAspireStore = Directory.CreateTempSubdirectory("foodsocial-apphost-tests-");
    private DistributedApplication? _app;

    internal AppHostAuthenticationHelper Auth { get; private set; } = null!;

    internal string RabbitMqConnectionString { get; private set; } = string.Empty;

    internal AppHostDatabaseVerifiers Databases { get; private set; } = null!;

    internal AppHostResourceDiscovery Resources { get; private set; } = null!;

    public CancellationTokenSource CreateDefaultCancellationTokenSource() => new(StartupTimeout);

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.FoodSocial_AppHost>();
        appHost.Configuration["Aspire:Store:Path"] = _tempAspireStore.FullName;
        appHost.Configuration["DcpPublisher:WaitForResourceCleanup"] = bool.TrueString;
        appHost.Configuration["Parameters:keycloak-bff-client-secret"] = TestKeycloakBffClientSecret;
        appHost.Configuration["Parameters:keycloak-demo-user-password"] = TestKeycloakDemoUserPassword;
        appHost.Configuration["Parameters:identity-internal-trust-shared-secret"] = TestIdentityInternalTrustSharedSecret;

        _app = await appHost.BuildAsync();
        await _app.StartAsync().WaitAsync(StartupTimeout);

        Resources = new AppHostResourceDiscovery(_app);
        Auth = new AppHostAuthenticationHelper(Resources);

        using var cts = CreateDefaultCancellationTokenSource();
        await WaitForRunningAsync("keycloak", cts.Token);
        await WaitForRunningAsync("postgres", cts.Token);
        await WaitForHealthyAsync("keycloak", cts.Token);
        await WaitForHealthyAsync("identity-api", cts.Token);
        await WaitForHealthyAsync("userprofile-api", cts.Token);
        await WaitForHealthyAsync("bff", cts.Token);

        var identityConnectionString = await _app.GetConnectionStringAsync("identitydb", cts.Token)
            ?? throw new InvalidOperationException("AppHost did not expose a connection string for 'identitydb'.");
        var userProfileConnectionString = await _app.GetConnectionStringAsync("userprofiledb", cts.Token)
            ?? throw new InvalidOperationException("AppHost did not expose a connection string for 'userprofiledb'.");
        RabbitMqConnectionString = await _app.GetConnectionStringAsync("rabbitmq", cts.Token)
            ?? throw new InvalidOperationException("AppHost did not expose a connection string for 'rabbitmq'.");
        Databases = new AppHostDatabaseVerifiers(identityConnectionString, userProfileConnectionString);

        await WaitForTableAsync(Databases.Identity, "identity_users", cts.Token);
        await WaitForTableAsync(Databases.Identity, "identity_role_assignments", cts.Token);
        await WaitForTableAsync(Databases.Identity, "identity_outbox_messages", cts.Token);
        await WaitForTableAsync(Databases.UserProfile, "user_profile_shells", cts.Token);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        if (_tempAspireStore.Exists)
        {
            _tempAspireStore.Delete(recursive: true);
        }
    }

    public HttpClient CreateUnauthenticatedClient(string resourceName, string? endpointName = null) =>
        Resources.CreateHttpClient(resourceName, endpointName);

    public async Task WaitForHealthyAsync(string resourceName, CancellationToken cancellationToken)
    {
        await EnsureApp().ResourceNotifications.WaitForResourceHealthyAsync(resourceName, cancellationToken)
            .WaitAsync(StartupTimeout, cancellationToken);
    }

    private async Task WaitForRunningAsync(string resourceName, CancellationToken cancellationToken)
    {
        await EnsureApp().ResourceNotifications.WaitForResourceAsync(resourceName, KnownResourceStates.Running, cancellationToken)
            .WaitAsync(StartupTimeout, cancellationToken);
    }

    private DistributedApplication EnsureApp() => _app ?? throw new InvalidOperationException("The AppHost test fixture has not been initialized.");

    private static async Task WaitForTableAsync(PostgreSqlDatabaseVerifier verifier, string tableName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(verifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await verifier.TableExistsAsync(tableName, cancellationToken: cancellationToken))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException($"Table '{tableName}' was not created before the AppHost fixture deadline.");
    }
}
