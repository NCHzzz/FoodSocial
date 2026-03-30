using Post.Service.Domain.Entities;

namespace Post.Service.Application.Interfaces;

public interface IPostService
{
    Task<Post?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Post>> GetAllAsync(CancellationToken ct = default);
    Task<Post> CreateAsync(CreatePostRequest request, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpdatePostRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public record CreatePostRequest();
public record UpdatePostRequest();

