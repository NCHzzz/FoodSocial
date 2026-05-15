using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application.Mapping;

namespace FoodSocial.Post.Application;

public sealed class PostQueryUseCase(IPostStore postStore, PostResponseMapper mapper)
{
    private readonly IPostStore _postStore = postStore ?? throw new ArgumentNullException(nameof(postStore));
    private readonly PostResponseMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<PostDetailResponse?> GetPostAsync(Guid postId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(postId, Guid.Empty);
        var post = await _postStore.GetPostAsync(postId, cancellationToken);
        return post is null ? null : _mapper.MapDetail(post);
    }

    public async Task<PostSearchResponse> SearchPostsAsync(PostSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var posts = await _postStore.SearchPostsAsync(criteria, cancellationToken);
        return new PostSearchResponse(posts.Select(_mapper.MapSummary).ToArray(), criteria.Limit);
    }

    public async Task<DiscoverFeedResponse> GetDiscoverFeedAsync(DiscoverFeedCriteria criteria, CancellationToken cancellationToken)
    {
        var posts = await _postStore.GetDiscoverFeedAsync(criteria, cancellationToken);
        return new DiscoverFeedResponse(posts.Select(_mapper.MapSummary).ToArray(), criteria.Limit);
    }
}
