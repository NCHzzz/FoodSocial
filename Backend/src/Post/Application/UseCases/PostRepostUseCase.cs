using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application.Mapping;

namespace FoodSocial.Post.Application;

public sealed class PostRepostUseCase(IPostStore postStore, PostResponseMapper mapper)
{
    private readonly IPostStore _postStore = postStore ?? throw new ArgumentNullException(nameof(postStore));
    private readonly PostResponseMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<RepostMutationResult> AddRepostAsync(Guid postId, CreatePostRepostRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (postId == Guid.Empty || request.ActorId == Guid.Empty)
        {
            return new RepostMutationResult(PostOperationStatus.Invalid, null, "Post id and actor id are required.");
        }

        if (request.Caption?.Trim() is { Length: > 280 })
        {
            return new RepostMutationResult(PostOperationStatus.Invalid, null, "Repost caption must be 280 characters or fewer.");
        }

        var repost = await _postStore.AddRepostAsync(postId, request.ActorId, request.Caption, cancellationToken);
        return repost is null
            ? new RepostMutationResult(PostOperationStatus.NotFound, null, "The requested post was not found.")
            : new RepostMutationResult(PostOperationStatus.Created, await _mapper.MapRepostAsync(repost, cancellationToken), null);
    }
}
