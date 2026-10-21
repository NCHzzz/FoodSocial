using FoodSocial.Contracts.Posts;

namespace FoodSocial.Post.Application.UseCases.Engagement;

public sealed class PostEngagementUseCase(IPostStore postStore)
{
    private readonly IPostStore _postStore = postStore ?? throw new ArgumentNullException(nameof(postStore));

    public async Task<PostLikeResponse?> AddLikeAsync(Guid postId, Guid actorId, CancellationToken cancellationToken)
    {
        if (postId == Guid.Empty || actorId == Guid.Empty) return null;
        var likeCount = await _postStore.AddLikeAsync(postId, actorId, cancellationToken);
        return likeCount is null ? null : new PostLikeResponse(postId, actorId, true, likeCount.Value);
    }

    public async Task<PostLikeResponse?> RemoveLikeAsync(Guid postId, Guid actorId, CancellationToken cancellationToken)
    {
        if (postId == Guid.Empty || actorId == Guid.Empty) return null;
        var likeCount = await _postStore.RemoveLikeAsync(postId, actorId, cancellationToken);
        return likeCount is null ? null : new PostLikeResponse(postId, actorId, false, likeCount.Value);
    }
}
