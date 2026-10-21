using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application.UseCases.Engagement;
using FoodSocial.Post.Application.UseCases.Reposts;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Post.Api.Controllers;

/// <summary>
/// Exposes the Post engagement endpoints (likes, reposts).
/// </summary>
[ApiController]
public sealed class PostEngagementController(
    PostEngagementUseCase engagementUseCase,
    PostRepostUseCase repostUseCase) : ControllerBase
{
    private readonly PostEngagementUseCase _engagementUseCase = engagementUseCase ?? throw new ArgumentNullException(nameof(engagementUseCase));
    private readonly PostRepostUseCase _repostUseCase = repostUseCase ?? throw new ArgumentNullException(nameof(repostUseCase));

    [HttpPost("api/posts/{postId:guid}/likes")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<PostLikeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PostLikeResponse>> LikePost(Guid postId, CancellationToken cancellationToken)
    {
        var response = await _engagementUseCase.AddLikeAsync(postId, PostInternalAuthorization.GetIdentityId(HttpContext), cancellationToken);
        return response is null
            ? Problem(title: "Post like failed", detail: "Post id and actor id must be valid, and the post must exist.", statusCode: StatusCodes.Status400BadRequest)
            : Ok(response);
    }

    [HttpDelete("api/posts/{postId:guid}/likes")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<PostLikeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PostLikeResponse>> UnlikePost(Guid postId, CancellationToken cancellationToken)
    {
        var response = await _engagementUseCase.RemoveLikeAsync(postId, PostInternalAuthorization.GetIdentityId(HttpContext), cancellationToken);
        return response is null
            ? Problem(title: "Post unlike failed", detail: "Post id and actor id must be valid, and the post must exist.", statusCode: StatusCodes.Status400BadRequest)
            : Ok(response);
    }

    [HttpPost("api/posts/{postId:guid}/repost")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<PostRepostResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PostRepostResponse>> Repost(
        Guid postId,
        [FromBody] CreatePostRepostRequest request,
        CancellationToken cancellationToken)
    {
        var securedRequest = request with { ActorId = PostInternalAuthorization.GetIdentityId(HttpContext) };
        var result = await _repostUseCase.AddRepostAsync(postId, securedRequest, cancellationToken);
        return result.Status switch
        {
            PostOperationStatus.Created => Created($"/api/posts/{postId}/reposts/{result.Repost!.Id}", result.Repost),
            PostOperationStatus.NotFound => Problem(title: "Post not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Invalid repost request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest)
        };
    }
}
