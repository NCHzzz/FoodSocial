using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Api;
using FoodSocial.Post.Application;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Post.Api.Controllers;

[ApiController]
public sealed class PostsController(PostService postService, PostMediaStorage mediaStorage, IConfiguration configuration) : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, string> AllowedImageContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    private readonly PostService _postService = postService ?? throw new ArgumentNullException(nameof(postService));
    private readonly PostMediaStorage _mediaStorage = mediaStorage ?? throw new ArgumentNullException(nameof(mediaStorage));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

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

    [HttpPost("api/media/uploads")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [RequestSizeLimit(10_000_000)]
    [ProducesResponseType<MediaUploadResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MediaUploadResponse>> UploadMedia(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return Problem(title: "Invalid media upload", detail: "A non-empty media file is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var validation = await ValidateImageUploadAsync(file, cancellationToken);
        if (validation.Error is not null)
        {
            return Problem(title: "Invalid media upload", detail: validation.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        var uploadId = Guid.NewGuid();
        try
        {
            var storedMedia = await _mediaStorage.StoreImageWithDerivativeAsync(uploadId, file, validation.Extension!, cancellationToken);
            var cleanupDueAtUtc = DateTimeOffset.UtcNow.AddDays(7);
            var response = new MediaUploadResponse(
                uploadId,
                storedMedia.OriginalUrl,
                storedMedia.ThumbnailUrl,
                validation.ContentType!,
                storedMedia.SizeBytes,
                cleanupDueAtUtc,
                "StoredPendingAttachmentWithDerivativePlaceholder");
            await _postService.RecordMediaUploadAsync(
                new RegisterMediaUploadCommand(
                    uploadId,
                    PostInternalAuthorization.GetIdentityId(HttpContext),
                    response.Url,
                    response.ThumbnailUrl,
                    response.ContentType,
                    response.SizeBytes,
                    response.CleanupDueAtUtc),
                cancellationToken);

            return Created(response.Url, response);
        }
        catch
        {
            await _mediaStorage.DeleteUploadAsync(uploadId);
            throw;
        }
    }

    [HttpGet("api/media/uploads/{uploadId:guid}/{variant}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetUploadedMedia(Guid uploadId, string variant, CancellationToken cancellationToken)
    {
        if (!string.Equals(variant, "original", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(variant, "thumbnail", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(title: "Invalid media variant", detail: "Media variant must be original or thumbnail.", statusCode: StatusCodes.Status400BadRequest);
        }

        var callerIdentityId = TryResolveTrustedCallerIdentity(HttpContext, _configuration, out var identityId)
            ? identityId
            : (Guid?)null;
        var upload = await _postService.GetDeliverableMediaUploadAsync(uploadId, callerIdentityId, cancellationToken);
        if (upload is null)
        {
            return Problem(title: "Media not found", detail: "The requested media upload was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var storedMedia = _mediaStorage.Find(uploadId, variant);
        if (storedMedia is null)
        {
            return Problem(title: "Media not found", detail: "The requested media upload was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        return PhysicalFile(storedMedia.Path, storedMedia.ContentType);
    }

    [HttpPost("api/media/uploads/cleanup")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<MediaCleanupResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MediaCleanupResponse>> CleanupMediaUploads([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var uploads = await _postService.ClaimMediaUploadsDueForCleanupAsync(DateTimeOffset.UtcNow, limit, cancellationToken);
        var deletedUploadIds = new List<Guid>(uploads.Count);
        foreach (var upload in uploads)
        {
            await _mediaStorage.DeleteUploadAsync(upload.Id);
            await _postService.MarkMediaUploadDeletedAsync(upload.Id, cancellationToken);
            deletedUploadIds.Add(upload.Id);
        }

        return Ok(new MediaCleanupResponse(uploads.Count, deletedUploadIds.Count, deletedUploadIds));
    }

    private static bool TryResolveTrustedCallerIdentity(HttpContext httpContext, IConfiguration configuration, out Guid identityId) =>
        PostInternalAuthorization.TryAuthorize(httpContext, configuration, requireModerator: false, out identityId, out _);

    private static async Task<ValidatedImageUpload> ValidateImageUploadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || string.IsNullOrWhiteSpace(file.FileName))
        {
            return ValidatedImageUpload.Invalid("A non-empty media file is required.");
        }

        if (file.Length > 10_000_000)
        {
            return ValidatedImageUpload.Invalid("Media uploads must be 10 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageContentTypes.TryGetValue(extension, out var expectedContentType)
            || !string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return ValidatedImageUpload.Invalid("Only JPEG, PNG, and WebP image uploads are supported in the MVP media pipeline.");
        }

        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header, cancellationToken);
        if (!HasExpectedImageSignature(extension, header.AsSpan(0, bytesRead)))
        {
            return ValidatedImageUpload.Invalid("Uploaded media bytes do not match the declared image type.");
        }

        return ValidatedImageUpload.Valid(extension, expectedContentType);
    }

    private static bool HasExpectedImageSignature(string extension, ReadOnlySpan<byte> header) =>
        extension switch
        {
            ".jpg" or ".jpeg" => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header.Length >= 8
                && header[0] == 0x89
                && header[1] == (byte)'P'
                && header[2] == (byte)'N'
                && header[3] == (byte)'G'
                && header[4] == 0x0D
                && header[5] == 0x0A
                && header[6] == 0x1A
                && header[7] == 0x0A,
            ".webp" => header.Length >= 12
                && header[0] == (byte)'R'
                && header[1] == (byte)'I'
                && header[2] == (byte)'F'
                && header[3] == (byte)'F'
                && header[8] == (byte)'W'
                && header[9] == (byte)'E'
                && header[10] == (byte)'B'
                && header[11] == (byte)'P',
            _ => false
        };

    private sealed record ValidatedImageUpload(string? Extension, string? ContentType, string? Error)
    {
        internal static ValidatedImageUpload Valid(string extension, string contentType) => new(extension, contentType, null);
        internal static ValidatedImageUpload Invalid(string error) => new(null, null, error);
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

    [HttpPost("api/posts/{postId:guid}/repost")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<PostRepostResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PostRepostResponse>> Repost(Guid postId, [FromBody] CreatePostRepostRequest request, CancellationToken cancellationToken)
    {
        var securedRequest = request with { ActorId = PostInternalAuthorization.GetIdentityId(HttpContext) };
        var result = await _postService.AddRepostAsync(postId, securedRequest, cancellationToken);
        return result.Status switch
        {
            PostOperationStatus.Created => Created($"/api/posts/{postId}/reposts/{result.Repost!.Id}", result.Repost),
            PostOperationStatus.NotFound => Problem(title: "Post not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Invalid repost request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest)
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

    [HttpGet("api/notifications")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<NotificationListResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationListResponse>> GetNotifications([FromQuery] int? limit, CancellationToken cancellationToken) =>
        Ok(await _postService.GetNotificationsAsync(PostInternalAuthorization.GetIdentityId(HttpContext), limit, cancellationToken));

    [HttpPatch("api/notifications/{notificationId:guid}/read")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<NotificationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationResponse>> MarkNotificationRead(Guid notificationId, CancellationToken cancellationToken)
    {
        var result = await _postService.MarkNotificationReadAsync(notificationId, PostInternalAuthorization.GetIdentityId(HttpContext), cancellationToken);
        return result.Status switch
        {
            PostOperationStatus.Updated => Ok(result.Notification),
            PostOperationStatus.NotFound => Problem(title: "Notification not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Invalid notification request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest)
        };
    }

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
