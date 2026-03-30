using Post.Service.Application.Interfaces;
using Post.Service.Domain.Entities;
using Post.Service.Domain.Interfaces;

namespace Post.Service.Application.Services;

public class PostService : IPostService
{
    private readonly IPostRepository _repository;

    public PostService(IPostRepository repository)
    {
        _repository = repository;
    }

    public async Task<Post?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync(id, ct);
    }

    public async Task<IEnumerable<Post>> GetAllAsync(CancellationToken ct = default)
    {
        return await _repository.GetAllAsync(ct);
    }

    public async Task<Post> CreateAsync(CreatePostRequest request, CancellationToken ct = default)
    {
        var entity = new Post { };
        return await _repository.CreateAsync(entity, ct);
    }

    public async Task UpdateAsync(Guid id, UpdatePostRequest request, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity is null)
            throw new KeyNotFoundException();
        await _repository.UpdateAsync(entity, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _repository.DeleteAsync(id, ct);
    }
}

