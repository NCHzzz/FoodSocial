using Post.Service.Domain.Entities;

namespace Post.Service.Domain.Interfaces;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Post>> GetAllAsync(CancellationToken ct = default);
    Task<Post> CreateAsync(Post entity, CancellationToken ct = default);
    Task UpdateAsync(Post entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

