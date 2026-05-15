using System.Xml.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace FoodSocial.AppHost.Tests;

public sealed class ServiceBoundaryArchitectureTests
{
    private static readonly ServiceBoundary[] Boundaries =
    [
        new("Identity", "identitydb", "identityDb", "IdentityDbContext"),
        new("UserProfile", "userprofiledb", "userProfileDb", "UserProfileDbContext"),
        new("Dish", "dishdb", "dishDb", "DishDbContext"),
        new("Place", "placedb", "placeDb", "PlaceDbContext"),
        new("Post", "postdb", "postDb", "PostDbContext")
    ];

    [Fact]
    public void BoundedServiceProjects_DoNotReferenceForeignServiceProjects()
    {
        var backendRoot = ResolveBackendRoot();
        var srcRoot = Path.Combine(backendRoot, "src");
        var violations = new List<string>();

        foreach (var projectPath in Directory.EnumerateFiles(srcRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var owningBoundary = TryGetBoundary(projectPath, srcRoot);
            if (owningBoundary is null)
            {
                continue;
            }

            var projectDirectory = Path.GetDirectoryName(projectPath)
                ?? throw new InvalidOperationException($"Project '{projectPath}' has no directory.");

            foreach (var referencePath in GetProjectReferencePaths(projectPath, projectDirectory))
            {
                var referencedBoundary = TryGetBoundary(referencePath, srcRoot);
                if (referencedBoundary is not null
                    && !string.Equals(owningBoundary.Name, referencedBoundary.Name, StringComparison.Ordinal))
                {
                    violations.Add($"{Relative(backendRoot, projectPath)} references foreign boundary project {Relative(backendRoot, referencePath)}");
                }
            }
        }

        violations.Should().BeEmpty("bounded services must compose across service boundaries through contracts, HTTP, or messaging instead of project-level access to another service implementation");
    }

    [Fact]
    public void BffProject_DoesNotReferenceBoundedServiceImplementationProjects()
    {
        var backendRoot = ResolveBackendRoot();
        var srcRoot = Path.Combine(backendRoot, "src");
        var projectPath = Path.Combine(srcRoot, "FoodSocial.Bff", "FoodSocial.Bff.csproj");
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Project '{projectPath}' has no directory.");

        var violations = GetProjectReferencePaths(projectPath, projectDirectory)
            .Where(referencePath => TryGetBoundary(referencePath, srcRoot) is not null)
            .Select(referencePath => $"BFF references bounded service implementation {Relative(backendRoot, referencePath)}")
            .ToArray();

        violations.Should().BeEmpty("the BFF may compose service APIs through HTTP clients and shared contracts, not direct service implementation references");
    }

    [Fact]
    public void ApiGatewayProject_DoesNotReferenceBoundedServicesOrDataProjects()
    {
        var backendRoot = ResolveBackendRoot();
        var srcRoot = Path.Combine(backendRoot, "src");
        var projectPath = Path.Combine(srcRoot, "FoodSocial.ApiGateway", "FoodSocial.ApiGateway.csproj");
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Project '{projectPath}' has no directory.");

        var violations = GetProjectReferencePaths(projectPath, projectDirectory)
            .Where(referencePath => TryGetBoundary(referencePath, srcRoot) is not null)
            .Select(referencePath => $"API Gateway references bounded service implementation {Relative(backendRoot, referencePath)}")
            .ToArray();

        violations.Should().BeEmpty("the API Gateway must stay an edge routing and policy host, not a service composition or data access layer");
    }

    [Fact]
    public void BoundedServiceSource_DoesNotUseForeignDbContextsOrPersistenceNamespaces()
    {
        var backendRoot = ResolveBackendRoot();
        var srcRoot = Path.Combine(backendRoot, "src");
        var violations = new List<string>();

        foreach (var boundary in Boundaries)
        {
            var boundaryRoot = Path.Combine(srcRoot, boundary.Name);
            var foreignBoundaries = Boundaries.Where(candidate => candidate.Name != boundary.Name).ToArray();

            foreach (var sourcePath in Directory.EnumerateFiles(boundaryRoot, "*.cs", SearchOption.AllDirectories))
            {
                var sourceText = File.ReadAllText(sourcePath);
                foreach (var foreignBoundary in foreignBoundaries)
                {
                    var forbiddenTokens = new[]
                    {
                        $"FoodSocial.{foreignBoundary.Name}.Infrastructure.Persistence",
                        $"FoodSocial.{foreignBoundary.Name}.Infrastructure",
                        foreignBoundary.DbContextTypeName,
                        $"DbSet<{foreignBoundary.Name}",
                        $"Set<{foreignBoundary.Name}"
                    };

                    foreach (var token in forbiddenTokens)
                    {
                        if (sourceText.Contains(token, StringComparison.Ordinal))
                        {
                            violations.Add($"{Relative(backendRoot, sourcePath)} contains foreign persistence token '{token}'");
                        }
                    }
                }
            }
        }

        violations.Should().BeEmpty("a service must never read or join against another service database through foreign DbContext or persistence namespace access");
    }

    [Fact]
    public void BoundedServiceSource_UsesOnlyOwnedDatabaseConnectionName()
    {
        var backendRoot = ResolveBackendRoot();
        var srcRoot = Path.Combine(backendRoot, "src");
        var violations = new List<string>();

        foreach (var boundary in Boundaries)
        {
            var boundaryRoot = Path.Combine(srcRoot, boundary.Name);
            var foreignDatabaseNames = Boundaries
                .Where(candidate => candidate.Name != boundary.Name)
                .Select(candidate => candidate.DatabaseName)
                .ToArray();

            foreach (var sourcePath in Directory.EnumerateFiles(boundaryRoot, "*.cs", SearchOption.AllDirectories))
            {
                var sourceText = File.ReadAllText(sourcePath);
                foreach (var foreignDatabaseName in foreignDatabaseNames)
                {
                    if (sourceText.Contains(foreignDatabaseName, StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add($"{Relative(backendRoot, sourcePath)} contains foreign database name '{foreignDatabaseName}'");
                    }
                }
            }
        }

        violations.Should().BeEmpty("runtime and design-time connection strings must point only at the owning service database");
    }

    [Fact]
    public void AppHost_WiresEachServiceOnlyToItsOwnedDatabaseResource()
    {
        var backendRoot = ResolveBackendRoot();
        var appHostSourcePath = Path.Combine(backendRoot, "src", "FoodSocial.AppHost", "AppHost.cs");
        var appHostSource = File.ReadAllText(appHostSourcePath);
        var violations = new List<string>();

        foreach (var boundary in Boundaries)
        {
            appHostSource.Should().Contain(
                $"var {boundary.AppHostDatabaseVariableName} = postgres.AddDatabase(\"{boundary.DatabaseName}\");",
                $"AppHost must declare the {boundary.Name} owned database resource explicitly");
        }

        foreach (var boundary in Boundaries)
        {
            var declaration = ExtractServiceDeclaration(appHostSource, boundary.Name);
            if (!declaration.Contains($".WithReference({boundary.AppHostDatabaseVariableName})", StringComparison.Ordinal))
            {
                violations.Add($"{boundary.Name} service is not wired to owned database resource '{boundary.AppHostDatabaseVariableName}'");
            }

            foreach (var foreignBoundary in Boundaries.Where(candidate => candidate.Name != boundary.Name))
            {
                if (declaration.Contains($".WithReference({foreignBoundary.AppHostDatabaseVariableName})", StringComparison.Ordinal))
                {
                    violations.Add($"{boundary.Name} service is wired to foreign database resource '{foreignBoundary.AppHostDatabaseVariableName}'");
                }
            }
        }

        var bffDeclaration = ExtractDeclaration(appHostSource, "var bff =", "var identityApi =");
        foreach (var boundary in Boundaries)
        {
            if (bffDeclaration.Contains($".WithReference({boundary.AppHostDatabaseVariableName})", StringComparison.Ordinal))
            {
                violations.Add($"BFF is wired directly to service database resource '{boundary.AppHostDatabaseVariableName}'");
            }
        }

        var apiGatewayDeclaration = ExtractDeclaration(appHostSource, "var apiGateway =", ";");
        foreach (var boundary in Boundaries)
        {
            if (apiGatewayDeclaration.Contains($".WithReference({boundary.AppHostDatabaseVariableName})", StringComparison.Ordinal))
            {
                violations.Add($"API Gateway is wired directly to service database resource '{boundary.AppHostDatabaseVariableName}'");
            }
        }

        violations.Should().BeEmpty("Aspire composition should prevent cross-service database access by only injecting each owned database into its owning service");
    }

    [Fact]
    public void AppHost_NonStableDockerResourceNames_DoNotUsePersistentContainersOrDataVolumes()
    {
        var backendRoot = ResolveBackendRoot();
        var appHostSourcePath = Path.Combine(backendRoot, "src", "FoodSocial.AppHost", "AppHost.cs");
        var appHostSource = File.ReadAllText(appHostSourcePath);

        Regex.Matches(
                appHostSource,
                @"else\s*\{\s*(postgres|rabbitMq|redis)\.WithDataVolume\(\);\s*\}",
                RegexOptions.Multiline)
            .Should()
            .BeEmpty("test AppHost runs disable stable Docker names and must not leave hash-suffixed persistent data volumes behind");

        Regex.Matches(
                appHostSource,
                @"^\s*(postgres|rabbitMq|redis|keycloak)\.WithLifetime\(ContainerLifetime\.Persistent\);",
                RegexOptions.Multiline)
            .Should()
            .BeEmpty("persistent container lifetime must be tied to the stable local-development container-name path, not to test-isolation resources");
    }

    [Fact]
    public void AppHost_WiresRabbitMqToAllBoundedServices()
    {
        var backendRoot = ResolveBackendRoot();
        var appHostSourcePath = Path.Combine(backendRoot, "src", "FoodSocial.AppHost", "AppHost.cs");
        var appHostSource = File.ReadAllText(appHostSourcePath);
        foreach (var boundary in Boundaries)
        {
            var declaration = ExtractServiceDeclaration(appHostSource, boundary.Name);
            declaration.Should().Contain(".WithReference(rabbitMq)", $"{boundary.Name} has RabbitMQ-backed integration event publishing");
            declaration.Should().Contain(".WaitFor(rabbitMq)", $"{boundary.Name} should wait for its required broker dependency");
        }
    }

    [Fact]
    public void AppHost_UsesKeycloakServiceClientForServiceToServiceTrust()
    {
        var backendRoot = ResolveBackendRoot();
        var appHostSourcePath = Path.Combine(backendRoot, "src", "FoodSocial.AppHost", "AppHost.cs");
        var appHostSource = File.ReadAllText(appHostSourcePath);

        appHostSource.Should().Contain("bffServiceClientSecret");
        appHostSource.Should().Contain("keycloak-bff-service-client-secret");
        appHostSource.Should().Contain(".WithEnvironment(\"KEYCLOAK_BFF_SERVICE_CLIENT_SECRET\", bffServiceClientSecret)");

        var bffDeclaration = ExtractDeclaration(appHostSource, "var bff =", "var identityApi =");
        var identityDeclaration = ExtractDeclaration(appHostSource, "var identityApi =", "var userProfileApi =");
        var postDeclaration = ExtractDeclaration(appHostSource, "var postApi =", ";");

        bffDeclaration.Should().Contain(".WithEnvironment(\"Keycloak__ServiceClientId\", \"foodsocial-bff-service\")");
        bffDeclaration.Should().Contain(".WithEnvironment(\"Keycloak__ServiceClientSecret\", bffServiceClientSecret)");
        identityDeclaration.Should().Contain(".WithEnvironment(\"Identity__TrustedServiceClients__BffClientId\", \"foodsocial-bff-service\")");
        postDeclaration.Should().Contain(".WithEnvironment(\"Post__TrustedServiceClients__BffClientId\", \"foodsocial-bff-service\")");
    }

    [Fact]
    public void AppHost_BffWaitsForReferencedRuntimeDependencies()
    {
        var backendRoot = ResolveBackendRoot();
        var appHostSourcePath = Path.Combine(backendRoot, "src", "FoodSocial.AppHost", "AppHost.cs");
        var appHostSource = File.ReadAllText(appHostSourcePath);
        var bffDeclaration = ExtractDeclaration(appHostSource, "var bff =", ";");
        var bffReferencesDeclaration = ExtractDeclaration(appHostSource, "bff.WithReference(identityApi)", ";");

        bffDeclaration.Should().Contain(".WaitFor(keycloak)");
        bffDeclaration.Should().Contain(".WaitFor(redis)");
        bffReferencesDeclaration.Should().Contain(".WaitFor(identityApi)");
        bffReferencesDeclaration.Should().Contain(".WaitFor(userProfileApi)");
        bffReferencesDeclaration.Should().Contain(".WaitFor(dishApi)");
        bffReferencesDeclaration.Should().Contain(".WaitFor(placeApi)");
        bffReferencesDeclaration.Should().Contain(".WaitFor(postApi)");
    }

    [Fact]
    public void AppHost_ApiGatewayDependsOnlyOnBff()
    {
        var backendRoot = ResolveBackendRoot();
        var appHostSourcePath = Path.Combine(backendRoot, "src", "FoodSocial.AppHost", "AppHost.cs");
        var appHostSource = File.ReadAllText(appHostSourcePath);
        var apiGatewayDeclaration = ExtractDeclaration(appHostSource, "var apiGateway =", ";");

        apiGatewayDeclaration.Should().Contain(".WithReference(bff)");
        apiGatewayDeclaration.Should().Contain(".WaitFor(bff)");
        apiGatewayDeclaration.Should().NotContain("identityApi");
        apiGatewayDeclaration.Should().NotContain("userProfileApi");
        apiGatewayDeclaration.Should().NotContain("dishApi");
        apiGatewayDeclaration.Should().NotContain("placeApi");
        apiGatewayDeclaration.Should().NotContain("postApi");
        apiGatewayDeclaration.Should().NotContain("rabbitMq");
        apiGatewayDeclaration.Should().NotContain("postgres");
    }

    [Fact]
    public void LocalKeycloakRealm_UsesApiGatewayBrowserRedirectOrigins()
    {
        var repoRoot = ResolveRepoRoot();
        var realmPath = Path.Combine(
            repoRoot,
            "Backend",
            "src",
            "FoodSocial.AppHost",
            "keycloak",
            "realms",
            "foodsocial-realm.json");
        var realmText = File.ReadAllText(realmPath);

        realmText.Should().Contain("http://localhost:4200/signin-oidc");
        realmText.Should().Contain("http://localhost:5280/signin-oidc");
        realmText.Should().Contain("https://localhost:7280/signin-oidc");
        realmText.Should().Contain("http://localhost:5280/signout-callback-oidc");
        realmText.Should().Contain("https://localhost:7280/signout-callback-oidc");
        realmText.Should().NotContain("http://localhost:5276/signin-oidc");
        realmText.Should().NotContain("https://localhost:7187/signin-oidc");
        realmText.Should().NotContain("http://localhost:5276/signout-callback-oidc");
        realmText.Should().NotContain("https://localhost:7187/signout-callback-oidc");
    }

    [Fact]
    public void DeploymentAssets_IncludePlaceServiceAndApiGatewayEverywhere()
    {
        var repoRoot = ResolveRepoRoot();
        var expectedPaths = new[]
        {
            Path.Combine(repoRoot, ".github", "workflows", "ci.yml"),
            Path.Combine(repoRoot, ".github", "workflows", "release-images.yml"),
            Path.Combine(repoRoot, ".github", "workflows", "deploy-kubernetes.yml"),
            Path.Combine(repoRoot, "ops", "helm", "foodsocial", "values.yaml")
        };

        File.Exists(Path.Combine(repoRoot, "Backend", "src", "Place", "Api", "Dockerfile"))
            .Should().BeTrue("Place.Api is an independently deployable service and must have a container build path");
        File.Exists(Path.Combine(repoRoot, "Backend", "src", "FoodSocial.ApiGateway", "Dockerfile"))
            .Should().BeTrue("API Gateway is the production edge service and must have a container build path");

        foreach (var path in expectedPaths)
        {
            var text = File.ReadAllText(path);
            text.Should().Contain("place-api", $"{Relative(repoRoot, path)} must publish, deploy, or configure Place.Api with the other bounded services");
            text.Should().Contain("api-gateway", $"{Relative(repoRoot, path)} must publish, deploy, or configure API Gateway as the production edge service");
        }
    }

    [Fact]
    public void HelmChart_PostMediaStorage_UsesPersistentVolumeClaimByDefault()
    {
        var repoRoot = ResolveRepoRoot();
        var valuesPath = Path.Combine(repoRoot, "ops", "helm", "foodsocial", "values.yaml");
        var servicesTemplatePath = Path.Combine(repoRoot, "ops", "helm", "foodsocial", "templates", "services.yaml");
        var valuesText = File.ReadAllText(valuesPath);
        var servicesTemplateText = File.ReadAllText(servicesTemplatePath);

        valuesText.Should().Contain("persistentVolumeClaim:");
        valuesText.Should().Contain("enabled: true");
        valuesText.Should().Contain("Post__Media__StoragePath");
        servicesTemplateText.Should().Contain("kind: PersistentVolumeClaim");
        servicesTemplateText.Should().Contain("persistentVolumeClaim:");
        servicesTemplateText.Should().NotContain("post-media\r\n          emptyDir: {}");
        servicesTemplateText.Should().NotContain("post-media\n          emptyDir: {}");
    }

    [Fact]
    public void ObservabilityAssets_CoverPostMediaAndModerationOperations()
    {
        var repoRoot = ResolveRepoRoot();
        var serviceDefaults = File.ReadAllText(Path.Combine(repoRoot, "Backend", "src", "FoodSocial.ServiceDefaults", "ServiceDefaultsExtensions.cs"));
        var postController = File.ReadAllText(Path.Combine(repoRoot, "Backend", "src", "Post", "Api", "Controllers", "PostsController.cs"));
        var alerts = File.ReadAllText(Path.Combine(repoRoot, "ops", "observability", "alerts", "foodsocial-alerts.yml"));
        var runbook = File.ReadAllText(Path.Combine(repoRoot, "ops", "observability", "README-production.md"));

        serviceDefaults.Should().Contain("FoodSocial.Application");
        postController.Should().Contain("PostApiMetrics");
        alerts.Should().Contain("foodsocial_post_media_cleanup_failures_total");
        alerts.Should().Contain("foodsocial_post_content_reports_created_total");
        alerts.Should().Contain("foodsocial_post_content_reports_resolved_total");
        runbook.Should().Contain("Post media cleanup failure");
        runbook.Should().Contain("Moderation backlog growth");
    }

    private static IReadOnlyList<string> GetProjectReferencePaths(string projectPath, string projectDirectory)
    {
        var document = XDocument.Load(projectPath);
        return document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFullPath(Path.Combine(projectDirectory, value!)))
            .ToArray();
    }

    private static ServiceBoundary? TryGetBoundary(string path, string srcRoot)
    {
        var relativePath = Path.GetRelativePath(srcRoot, path);
        if (relativePath.StartsWith("..", StringComparison.Ordinal))
        {
            return null;
        }

        var firstSegment = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return Boundaries.FirstOrDefault(boundary => string.Equals(boundary.Name, firstSegment, StringComparison.Ordinal));
    }

    private static string ExtractServiceDeclaration(string sourceText, string boundaryName)
    {
        var variableName = char.ToLowerInvariant(boundaryName[0]) + boundaryName[1..] + "Api";
        return ExtractDeclaration(sourceText, $"var {variableName} =", ";");
    }

    private static string ExtractDeclaration(string sourceText, string startMarker, string endMarker)
    {
        var start = sourceText.IndexOf(startMarker, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"source should contain declaration start '{startMarker}'");

        var end = sourceText.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start, $"source should contain declaration end '{endMarker}' after '{startMarker}'");

        return sourceText[start..(end + endMarker.Length)];
    }

    private static string ResolveBackendRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FoodSocial.slnx"))
                && Directory.Exists(Path.Combine(directory.FullName, "src"))
                && Directory.Exists(Path.Combine(directory.FullName, "tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not resolve Backend root from test output directory.");
    }

    private static string ResolveRepoRoot()
    {
        var backendRoot = ResolveBackendRoot();
        return Directory.GetParent(backendRoot)?.FullName
            ?? throw new InvalidOperationException("Could not resolve repository root from Backend root.");
    }

    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

    private sealed record ServiceBoundary(
        string Name,
        string DatabaseName,
        string AppHostDatabaseVariableName,
        string DbContextTypeName);
}
