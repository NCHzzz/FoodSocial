using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application.UseCases.Publishing;
using FoodSocial.Post.Application.UseCases.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Post.Api.Controllers;

/// <summary>
/// Exposes the canonical Post lifecycle endpoints (create, get, search, discover feed).
/// </summary>
[ApiController]
public sealed class PostsController(
    PostPublishingUseCase publishingUseCase,
    PostQueryUseCase queryUseCase) : ControllerBase
{
    private readonly PostPublishingUseCase _publishingUseCase = publishingUseCase ?? throw new ArgumentNullException(nameof(publishingUseCase));
    private readonly PostQueryUseCase _queryUseCase = queryUseCase ?? throw new ArgumentNullException(nameof(queryUseCase));

    [HttpPost("api/posts")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<PostDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostDetailResponse>> CreatePost([FromBody] CreatePostRequest request, CancellationToken cancellationToken)
    {
        var securedRequest = request with { AuthorId = PostInternalAuthorization.GetIdentityId(HttpContext) };
        var result = await _publishingUseCase.CreatePostAsync(securedRequest, cancellationToken);
        return result.Status == PostOperationStatus.Created
            ? CreatedAtAction(nameof(GetPost), new { postId = result.Post!.Id }, result.Post)
            : Problem(title: "Invalid post request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpGet("api/posts/{postId:guid}")]
    [ProducesResponseType<PostDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDetailResponse>> GetPost(Guid postId, CancellationToken cancellationToken)
    {
        if (postId == Guid.Empty) return Problem(title: "Invalid post id", detail: "Post id must not be empty.", statusCode: StatusCodes.Status400BadRequest);
        var post = await _queryUseCase.GetPostAsync(postId, cancellationToken);
        return post is null
            ? Problem(title: "Post not found", detail: "The requested post was not found.", statusCode: StatusCodes.Status404NotFound)
            : Ok(post);
    }

    [HttpGet("api/posts/search")]
    [ProducesResponseType<PostSearchResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PostSearchResponse>> SearchPosts(
        [FromQuery(Name = "authorId")] Guid[]? authorIds,
        [FromQuery] Guid? dishId,
        [FromQuery] Guid? placeId,
        [FromQuery] string? tag,
        [FromQuery] string? query,
        [FromQuery] bool includeHidden,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var response = await _queryUseCase.SearchPostsAsync(
            PostSearchCriteria.Create(authorIds, dishId, placeId, tag, query, includeHidden, limit),
            cancellationToken);
        return Ok(response);
    }

    [HttpGet("api/feed/discover")]
    [ProducesResponseType<DiscoverFeedResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DiscoverFeedResponse>> GetDiscoverFeed([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var response = await _queryUseCase.GetDiscoverFeedAsync(DiscoverFeedCriteria.Create(limit), cancellationToken);
        return Ok(response);
    }
}
