using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Api;
using FoodSocial.Post.Application;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Post.Api.Controllers;

[ApiController]
public sealed class PostsController(PostService postService) : ControllerBase
{
    private readonly PostService _postService = postService ?? throw new ArgumentNullException(nameof(postService));

    [HttpPost("api/posts")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<PostDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostDetailResponse>> CreatePost([FromBody] CreatePostRequest request, CancellationToken cancellationToken)
    {
        var securedRequest = request with { AuthorId = PostInternalAuthorization.GetIdentityId(HttpContext) };
        var result = await _postService.CreatePostAsync(securedRequest, cancellationToken);
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
        var post = await _postService.GetPostAsync(postId, cancellationToken);
        return post is null ? Problem(title: "Post not found", detail: "The requested post was not found.", statusCode: StatusCodes.Status404NotFound) : Ok(post);
    }

    [HttpGet("api/posts/search")]
    [ProducesResponseType<PostSearchResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PostSearchResponse>> SearchPosts([FromQuery] Guid? authorId, [FromQuery] Guid? dishId, [FromQuery] Guid? placeId, [FromQuery] string? tag, [FromQuery] string? query, [FromQuery] bool includeHidden, [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var response = await _postService.SearchPostsAsync(PostSearchCriteria.Create(authorId, dishId, placeId, tag, query, includeHidden, limit), cancellationToken);
        return Ok(response);
    }

    [HttpGet("api/feed/discover")]
    [ProducesResponseType<DiscoverFeedResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DiscoverFeedResponse>> GetDiscoverFeed([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var response = await _postService.GetDiscoverFeedAsync(DiscoverFeedCriteria.Create(limit), cancellationToken);
        return Ok(response);
    }

    [HttpPost("api/posts/{postId:guid}/likes")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<PostLikeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PostLikeResponse>> LikePost(Guid postId, CancellationToken cancellationToken)
    {
        var response = await _postService.AddLikeAsync(postId, PostInternalAuthorization.GetIdentityId(HttpContext), cancellationToken);
        return response is null ? Problem(title: "Post like failed", detail: "Post id and actor id must be valid, and the post must exist.", statusCode: StatusCodes.Status400BadRequest) : Ok(response);
    }

    [HttpDelete("api/posts/{postId:guid}/likes")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<PostLikeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PostLikeResponse>> UnlikePost(Guid postId, CancellationToken cancellationToken)
    {
        var response = await _postService.RemoveLikeAsync(postId, PostInternalAuthorization.GetIdentityId(HttpContext), cancellationToken);
        return response is null ? Problem(title: "Post unlike failed", detail: "Post id and actor id must be valid, and the post must exist.", statusCode: StatusCodes.Status400BadRequest) : Ok(response);
    }

    [HttpPost("api/posts/{postId:guid}/comments")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<PostCommentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PostCommentResponse>> CreateComment(Guid postId, [FromBody] CreatePostCommentRequest request, CancellationToken cancellationToken)
    {
        var securedRequest = request with { AuthorId = PostInternalAuthorization.GetIdentityId(HttpContext) };
        var result = await _postService.AddCommentAsync(postId, securedRequest, cancellationToken);
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
        var result = await _postService.DeleteCommentAsync(commentId, PostInternalAuthorization.GetIdentityId(HttpContext), cancellationToken);
        return result.Status switch
        {
            PostOperationStatus.Deleted => Ok(result.Comment),
            PostOperationStatus.NotFound => Problem(title: "Comment not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            PostOperationStatus.Forbidden => Problem(title: "Comment deletion forbidden", detail: result.Error, statusCode: StatusCodes.Status403Forbidden),
            _ => Problem(title: "Invalid comment deletion request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest)
        };
    }

    [HttpPost("api/posts/{postId:guid}/report")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<ContentReportResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ContentReportResponse>> ReportPost(Guid postId, [FromBody] CreateContentReportRequest request, CancellationToken cancellationToken)
    {
        var securedRequest = request with { ReporterId = PostInternalAuthorization.GetIdentityId(HttpContext) };
        return await ReportAsync(() => _postService.ReportPostAsync(postId, securedRequest, cancellationToken));
    }

    [HttpPost("api/comments/{commentId:guid}/report")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<ContentReportResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ContentReportResponse>> ReportComment(Guid commentId, [FromBody] CreateContentReportRequest request, CancellationToken cancellationToken)
    {
        var securedRequest = request with { ReporterId = PostInternalAuthorization.GetIdentityId(HttpContext) };
        return await ReportAsync(() => _postService.ReportCommentAsync(commentId, securedRequest, cancellationToken));
    }

    [HttpGet("api/admin/reports")]
    [ServiceFilter(typeof(PostModeratorAuthorizationFilter))]
    [ProducesResponseType<AdminReportsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminReportsResponse>> GetReports([FromQuery] int? limit, CancellationToken cancellationToken) => Ok(await _postService.GetReportsAsync(limit, cancellationToken));

    [HttpPost("api/admin/reports/{reportId:guid}/resolve")]
    [ServiceFilter(typeof(PostModeratorAuthorizationFilter))]
    [ProducesResponseType<ContentReportResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ContentReportResponse>> ResolveReport(Guid reportId, [FromBody] ResolveContentReportRequest request, CancellationToken cancellationToken)
    {
        var securedRequest = request with { ModeratorId = PostInternalAuthorization.GetIdentityId(HttpContext) };
        var result = await _postService.ResolveReportAsync(reportId, securedRequest, cancellationToken);
        return result.Status switch
        {
            PostOperationStatus.Resolved => Ok(result.Report),
            PostOperationStatus.NotFound => Problem(title: "Report not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Invalid report resolution request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest)
        };
    }

    private async Task<ActionResult<ContentReportResponse>> ReportAsync(Func<Task<ReportMutationResult>> action)
    {
        var result = await action();
        return result.Status switch
        {
            PostOperationStatus.Created => Created($"/api/admin/reports/{result.Report!.Id}", result.Report),
            PostOperationStatus.NotFound => Problem(title: "Report target not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Invalid report request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest)
        };
    }
}
