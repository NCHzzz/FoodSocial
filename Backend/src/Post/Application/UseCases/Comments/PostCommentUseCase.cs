using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application.Mapping;

namespace FoodSocial.Post.Application.UseCases.Comments;

public sealed class PostCommentUseCase(IPostStore postStore, PostResponseMapper mapper)
{
    private readonly IPostStore _postStore = postStore ?? throw new ArgumentNullException(nameof(postStore));
    private readonly PostResponseMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<CommentMutationResult> AddCommentAsync(Guid postId, CreatePostCommentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (postId == Guid.Empty || request.AuthorId == Guid.Empty || string.IsNullOrWhiteSpace(request.Body)) return new CommentMutationResult(PostOperationStatus.Invalid, null, "Post id, author id, and comment body are required.");
        var comment = await _postStore.AddCommentAsync(postId, request.AuthorId, request.Body.Trim(), cancellationToken);
        return comment is null ? new CommentMutationResult(PostOperationStatus.NotFound, null, "The requested post was not found.") : new CommentMutationResult(PostOperationStatus.Created, _mapper.MapComment(comment), null);
    }

    public async Task<CommentMutationResult> DeleteCommentAsync(Guid commentId, Guid actorId, CancellationToken cancellationToken)
    {
        if (commentId == Guid.Empty || actorId == Guid.Empty) return new CommentMutationResult(PostOperationStatus.Invalid, null, "Comment id and actor id are required.");
        var existing = await _postStore.GetCommentAsync(commentId, cancellationToken);
        if (existing is null) return new CommentMutationResult(PostOperationStatus.NotFound, null, "The requested comment was not found.");
        if (existing.AuthorId != actorId) return new CommentMutationResult(PostOperationStatus.Forbidden, null, "Only the comment owner can delete the comment.");
        var deleted = await _postStore.SoftDeleteCommentAsync(commentId, actorId, cancellationToken);
        return new CommentMutationResult(PostOperationStatus.Deleted, _mapper.MapComment(deleted!), null);
    }
}
