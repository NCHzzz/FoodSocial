using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application.UseCases.Comments;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Post.Api.Controllers;

/// <summary>
/// Exposes the Post comment endpoints (create, delete).
/// </summary>
[ApiController]
public sealed class PostCommentsController(PostCommentUseCase commentUseCase) : ControllerBase
{
    private readonly PostCommentUseCase _commentUseCase = commentUseCase ?? throw new ArgumentNullException(nameof(commentUseCase));

    [HttpPost("api/posts/{postId:guid}/comments")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<PostCommentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PostCommentResponse>> CreateComment(
        Guid postId,
        [FromBody] CreatePostCommentRequest request,
        CancellationToken cancellationToken)
    {
        var securedRequest = request with { AuthorId = PostInternalAuthorization.GetIdentityId(HttpContext) };
        var result = await _commentUseCase.AddCommentAsync(postId, securedRequest, cancellationToken);
        return result.Status switch
        {
            PostOperationStatus.Created => Created($"/api/comments/{result.Comment!.Id}", result.Comment),
            PostOperationStatus.NotFound => Problem(title: "Post not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Invalid comment request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest)
        };
    }

    [HttpDelete("api/comments/{commentId:guid}")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<PostCommentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PostCommentResponse>> DeleteComment(Guid commentId, CancellationToken cancellationToken)
    {
        var result = await _commentUseCase.DeleteCommentAsync(commentId, PostInternalAuthorization.GetIdentityId(HttpContext), cancellationToken);
        return result.Status switch
        {
            PostOperationStatus.Deleted => Ok(result.Comment),
            PostOperationStatus.NotFound => Problem(title: "Comment not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            PostOperationStatus.Forbidden => Problem(title: "Comment deletion forbidden", detail: result.Error, statusCode: StatusCodes.Status403Forbidden),
            _ => Problem(title: "Invalid comment deletion request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest)
        };
    }
}
