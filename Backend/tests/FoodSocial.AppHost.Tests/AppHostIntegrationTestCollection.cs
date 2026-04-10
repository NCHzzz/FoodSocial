using Xunit;

namespace FoodSocial.AppHost.Tests;

[CollectionDefinition(Name)]
public sealed class AppHostIntegrationTestCollection : ICollectionFixture<AppHostFixture>
{
    public const string Name = "AppHost integration tests";
}
