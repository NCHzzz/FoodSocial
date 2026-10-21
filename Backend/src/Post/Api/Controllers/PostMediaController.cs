using FoodSocial.Contracts.Posts;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Post.Api.Controllers;


/// <summary>
/// Exposes the Post media upload and cleanup endpoints.
/// </summary>
[ApiController]
public sealed class PostMediaController(PostMediaUploadWorkflow mediaUploadWorkflow) : ControllerBase
{
    private readonly PostMediaUploadWorkflow _mediaUploadWorkflow = mediaUploadWorkflow ?? throw new ArgumentNullException(nameof(mediaUploadWorkflow));

    [HttpPost("api/media/uploads")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [RequestSizeLimit(10_000_000)]
    [ProducesResponseType<MediaUploadResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MediaUploadResponse>> UploadMedia(IFormFile? file, CancellationToken cancellationToken)
    {
        var result = await _mediaUploadWorkflow.UploadAsync(PostInternalAuthorization.GetIdentityId(HttpContext), file, cancellationToken);
        return result.IsCreated
            ? Created(result.Response!.Url, result.Response)
            : Problem(title: "Invalid media upload", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("api/media/uploads/cleanup")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<MediaCleanupResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MediaCleanupResponse>> CleanupMediaUploads([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var response = await _mediaUploadWorkflow.CleanupAsync(limit, cancellationToken);
        return Ok(response);
    }
}
