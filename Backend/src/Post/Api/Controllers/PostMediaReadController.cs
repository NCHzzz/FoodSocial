using FoodSocial.Post.Application.UseCases.Media;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Post.Api.Controllers;

/// <summary>
/// Exposes the Post media read endpoint that resolves a media upload record.
/// The actual byte stream is served by <see cref="PostMediaReadController"/>.
/// </summary>
[ApiController]
public sealed class PostMediaReadController(
    PostMediaUploadUseCase mediaUploadUseCase,
    PostMediaStorage mediaStorage,
    IConfiguration configuration,
    ILogger<PostMediaReadController> logger) : ControllerBase
{
    private readonly PostMediaUploadUseCase _mediaUploadUseCase = mediaUploadUseCase ?? throw new ArgumentNullException(nameof(mediaUploadUseCase));
    private readonly PostMediaStorage _mediaStorage = mediaStorage ?? throw new ArgumentNullException(nameof(mediaStorage));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ILogger<PostMediaReadController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

        var callerIdentityId = PostInternalAuthorization.TryAuthorize(HttpContext, _configuration, _logger, requireModerator: false, out var identityId, out _)
            ? identityId
            : (Guid?)null;
        var upload = await _mediaUploadUseCase.GetDeliverableMediaUploadAsync(uploadId, callerIdentityId, cancellationToken);
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
}
