using Post.Service.Domain.Entities;
using Post.Service.Domain.Interfaces;

namespace Post.Service.Infrastructure.Repositories;

public class PostRepository : IPostRepository
{
    public async Task<Post?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return null;
    }

    public async Task<IEnumerable<Post>> GetAllAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return Enumerable.Empty<Post>();
    }

    public async Task<Post> CreateAsync(Post entity, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return entity;
    }

    public async Task UpdateAsync(Post entity, CancellationToken ct = default)
    {
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await Task.CompletedTask;
    }
}

