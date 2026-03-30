using Post.Service.Application.Interfaces;
using Post.Service.Application.Services;
using Post.Service.Domain.Entities;
using Post.Service.Domain.Interfaces;
using Moq;

namespace Post.Service.UnitTests;

public class PostServiceTests
{
    private readonly Mock<IPostRepository> _mockRepo;
    private readonly PostService _service;

    public PostServiceTests()
    {
        _mockRepo = new Mock<IPostRepository>();
        _service = new PostService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenExists()
    {
        var id = Guid.NewGuid();
        var expected = new Post { Id = id };
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _service.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Post?)null);

        var result = await _service.GetByIdAsync(id);

        Assert.Null(result);
    }
}

