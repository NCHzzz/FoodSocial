using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace FoodSocial.AppHost.Tests;

public sealed class ServiceBoundaryArchitectureTests
{
    private static readonly ServiceBoundary[] Boundaries =
    [
        new("Identity", "identitydb", "identityDb", "IdentityDbContext"),
        new("UserProfile", "userprofiledb", "userprofileDb", "UserProfileDbContext"),
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

        violations.Should().BeEmpty("Aspire composition should prevent cross-service database access by only injecting each owned database into its owning service");
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

    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

    private sealed record ServiceBoundary(
        string Name,
        string DatabaseName,
        string AppHostDatabaseVariableName,
        string DbContextTypeName);
}
