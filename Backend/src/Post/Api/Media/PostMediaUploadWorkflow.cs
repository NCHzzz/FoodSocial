using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application.UseCases.Media;

using FoodSocial.Post.Api.Observability;

namespace FoodSocial.Post.Api.Media;

public sealed class PostMediaUploadWorkflow(
    PostMediaUploadUseCase mediaUploadUseCase,
    PostMediaStorage mediaStorage,


    PostMediaUploadValidator validator,
    PostApiMetrics metrics,
    ILogger<PostMediaUploadWorkflow> logger)
{
    private readonly PostMediaUploadUseCase _mediaUploadUseCase = mediaUploadUseCase ?? throw new ArgumentNullException(nameof(mediaUploadUseCase));
    private readonly PostMediaStorage _mediaStorage = mediaStorage ?? throw new ArgumentNullException(nameof(mediaStorage));
    private readonly PostMediaUploadValidator _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly PostApiMetrics _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    private readonly ILogger<PostMediaUploadWorkflow> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<PostMediaUploadWorkflowResult> UploadAsync(Guid ownerId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            _metrics.RecordMediaUploadRejected("missing_file");
            return PostMediaUploadWorkflowResult.Invalid("A non-empty media file is required.");
        }

        var validation = await _validator.ValidateImageUploadAsync(file, cancellationToken);
        if (validation.Error is not null)
        {
            _metrics.RecordMediaUploadRejected(validation.Reason!);
            return PostMediaUploadWorkflowResult.Invalid(validation.Error);
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

            await _mediaUploadUseCase.RecordMediaUploadAsync(
                new RegisterMediaUploadCommand(
                    uploadId,
                    ownerId,
                    response.Url,
                    response.ThumbnailUrl,
                    response.ContentType,
                    response.SizeBytes,
                    response.CleanupDueAtUtc),
                cancellationToken);

            _metrics.RecordMediaUploadAccepted(response.ContentType);
            return PostMediaUploadWorkflowResult.Created(response);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _metrics.RecordMediaUploadRejected("storage_or_lifecycle_failure");
            _logger.LogError(exception, "Post media upload {UploadId} failed after storage or lifecycle write started.", uploadId);
            await _mediaStorage.DeleteUploadAsync(uploadId);
            throw;
        }
    }

    public async Task<MediaCleanupResponse> CleanupAsync(int? limit, CancellationToken cancellationToken)
    {
        var uploads = await _mediaUploadUseCase.ClaimMediaUploadsDueForCleanupAsync(DateTimeOffset.UtcNow, limit, cancellationToken);
        _metrics.RecordMediaCleanupClaimed(uploads.Count);
        var deletedUploadIds = new List<Guid>(uploads.Count);

        foreach (var upload in uploads)
        {
            try
            {
                await _mediaStorage.DeleteUploadAsync(upload.Id);
                await _mediaUploadUseCase.MarkMediaUploadDeletedAsync(upload.Id, cancellationToken);
                deletedUploadIds.Add(upload.Id);
                _metrics.RecordMediaCleanupDeleted();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _metrics.RecordMediaCleanupFailure();
                _logger.LogError(exception, "Post media cleanup failed for upload {UploadId}.", upload.Id);
            }
        }

        return new MediaCleanupResponse(uploads.Count, deletedUploadIds.Count, deletedUploadIds);
    }
}

public sealed record PostMediaUploadWorkflowResult(MediaUploadResponse? Response, string? Error)
{
    public bool IsCreated => Response is not null;

    public static PostMediaUploadWorkflowResult Created(MediaUploadResponse response) => new(response, null);

    public static PostMediaUploadWorkflowResult Invalid(string error) => new(null, error);
}
