using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Npgsql;
using System.Net;
using Xunit;

namespace FoodSocial.AppHost.Tests;

public sealed class AppHostFixture : IAsyncLifetime
{
    private const string UseStableDockerResourceNamesEnvironmentVariable = "FoodSocial__AppHost__UseStableDockerResourceNames";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);
    internal const string TestKeycloakBffClientSecret = "foodsocial-bff-test-secret";
    internal const string TestKeycloakBffServiceClientSecret = "foodsocial-bff-service-test-secret";
    internal const string TestKeycloakDemoUserPassword = "Pass@word1";

    private readonly DirectoryInfo _tempAspireStore = Directory.CreateTempSubdirectory("foodsocial-apphost-tests-");
    private DistributedApplication? _app;

    internal AppHostAuthenticationHelper Auth { get; private set; } = null!;

    internal string RabbitMqConnectionString { get; private set; } = string.Empty;

    internal AppHostDatabaseVerifiers Databases { get; private set; } = null!;

    internal AppHostResourceDiscovery Resources { get; private set; } = null!;

    public CancellationTokenSource CreateDefaultCancellationTokenSource() => new(StartupTimeout);

    public async Task InitializeAsync()
    {
        var previousUseStableDockerResourceNames = Environment.GetEnvironmentVariable(UseStableDockerResourceNamesEnvironmentVariable);
        IDistributedApplicationTestingBuilder appHost;

        try
        {
            Environment.SetEnvironmentVariable(UseStableDockerResourceNamesEnvironmentVariable, bool.FalseString);
            appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.FoodSocial_AppHost>();
        }
        finally
        {
            Environment.SetEnvironmentVariable(UseStableDockerResourceNamesEnvironmentVariable, previousUseStableDockerResourceNames);
        }

        appHost.Configuration["Aspire:Store:Path"] = _tempAspireStore.FullName;
        appHost.Configuration["DcpPublisher:WaitForResourceCleanup"] = bool.TrueString;
        appHost.Configuration["Parameters:keycloak-bff-client-secret"] = TestKeycloakBffClientSecret;
        appHost.Configuration["Parameters:keycloak-bff-service-client-secret"] = TestKeycloakBffServiceClientSecret;
        appHost.Configuration["Parameters:keycloak-demo-user-password"] = TestKeycloakDemoUserPassword;

        _app = await appHost.BuildAsync();
        await _app.StartAsync().WaitAsync(StartupTimeout);

        Resources = new AppHostResourceDiscovery(_app);
        Auth = new AppHostAuthenticationHelper(Resources);

        using var cts = CreateDefaultCancellationTokenSource();
        await WaitForRunningAsync("keycloak", cts.Token);
        await WaitForRunningAsync("postgres", cts.Token);
        await WaitForRunningAsync("rabbitmq", cts.Token);
        await WaitForHealthyAsync("keycloak", cts.Token);
        await WaitForHealthyAsync("postgres", cts.Token);
        await WaitForHealthyAsync("rabbitmq", cts.Token);
        await WaitForHttpOkAsync("identity-api", ["https", "http"], "/alive", cts.Token);
        await WaitForHttpOkAsync("userprofile-api", ["https", "http"], "/alive", cts.Token);
        await WaitForHttpOkAsync("bff", ["https", "http"], "/alive", cts.Token);
        await WaitForHttpOkAsync("api-gateway", ["https", "http"], "/alive", cts.Token);

        var identityConnectionString = await _app.GetConnectionStringAsync("identitydb", cts.Token)
            ?? throw new InvalidOperationException("AppHost did not expose a connection string for 'identitydb'.");
        var userProfileConnectionString = await _app.GetConnectionStringAsync("userprofiledb", cts.Token)
            ?? throw new InvalidOperationException("AppHost did not expose a connection string for 'userprofiledb'.");
        RabbitMqConnectionString = await _app.GetConnectionStringAsync("rabbitmq", cts.Token)
            ?? throw new InvalidOperationException("AppHost did not expose a connection string for 'rabbitmq'.");
        Databases = new AppHostDatabaseVerifiers(identityConnectionString, userProfileConnectionString);

        await WaitForDatabaseConnectionAsync(Databases.Identity, "identitydb", cts.Token);
        await WaitForDatabaseConnectionAsync(Databases.UserProfile, "userprofiledb", cts.Token);

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

    private async Task WaitForTableAsync(PostgreSqlDatabaseVerifier verifier, string tableName, CancellationToken cancellationToken)
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

        var migrationHistoryExists = await verifier.TableExistsAsync("__UserProfileMigrationsHistory", cancellationToken: cancellationToken);
        var tables = await verifier.ListTablesAsync(cancellationToken: cancellationToken);

        throw new TimeoutException(
            $"Table '{tableName}' was not created before the AppHost fixture deadline. " +
            $"__UserProfileMigrationsHistory exists: {migrationHistoryExists}. " +
            $"Visible tables: [{string.Join(", ", tables)}].");
    }

    private async Task WaitForHttpOkAsync(string resourceName, IReadOnlyList<string> endpointNames, string path, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(StartupTimeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            foreach (var endpointName in endpointNames)
            {
                try
                {
                    var endpoint = Resources.GetEndpoint(resourceName, endpointName);
                    using var handler = new HttpClientHandler
                    {
                        AllowAutoRedirect = false,
                        ServerCertificateCustomValidationCallback = endpoint.Scheme == Uri.UriSchemeHttps
                            && string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                            ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                            : null
                    };
                    using var client = new HttpClient(handler, disposeHandler: true)
                    {
                        BaseAddress = endpoint
                    };
                    client.Timeout = TimeSpan.FromSeconds(5);

                    using var response = await client.GetAsync(path, cancellationToken);
                    if (response.IsSuccessStatusCode
                        || response.StatusCode == HttpStatusCode.TemporaryRedirect
                        || response.StatusCode == HttpStatusCode.PermanentRedirect
                        || response.StatusCode == HttpStatusCode.RedirectKeepVerb)
                    {
                        return;
                    }
                }
                catch (InvalidOperationException)
                {
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                }
                catch (HttpRequestException)
                {
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException($"HTTP endpoint '{resourceName}{path}' did not return 200 before the AppHost fixture deadline.");
    }

    private static async Task WaitForDatabaseConnectionAsync(PostgreSqlDatabaseVerifier verifier, string databaseName, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(StartupTimeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                if (await verifier.CanConnectAsync(cancellationToken))
                {
                    return;
                }
            }
            catch (NpgsqlException)
            {
            }
            catch (TimeoutException)
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException($"Database '{databaseName}' did not accept connections before the AppHost fixture deadline.");
    }
}
