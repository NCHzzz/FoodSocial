using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Post.Service.IntegrationTests;

public class PostIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PostIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSuccessStatusCode()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/Post");
        response.EnsureSuccessStatusCode();
    }
}

