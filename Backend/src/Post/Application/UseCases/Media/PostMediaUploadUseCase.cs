using FoodSocial.Post.Domain;

namespace FoodSocial.Post.Application.UseCases.Media;

public sealed class PostMediaUploadUseCase(IPostStore postStore)
{
    private readonly IPostStore _postStore = postStore ?? throw new ArgumentNullException(nameof(postStore));

    public async Task<PostMediaUploadRecord?> GetDeliverableMediaUploadAsync(Guid uploadId, Guid? callerIdentityId, CancellationToken cancellationToken) =>
        await _postStore.GetDeliverableMediaUploadAsync(uploadId, callerIdentityId, cancellationToken);

    public async Task RecordMediaUploadAsync(RegisterMediaUploadCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.UploadId == Guid.Empty || command.OwnerId == Guid.Empty)
        {
            throw new ArgumentException("Upload id and owner id are required.", nameof(command));
        }

        await _postStore.RegisterMediaUploadAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<PostMediaUploadRecord>> ClaimMediaUploadsDueForCleanupAsync(DateTimeOffset utcNow, int? limit, CancellationToken cancellationToken)
    {
        var boundedLimit = Math.Clamp(limit ?? 100, 1, 500);
        return await _postStore.ClaimMediaUploadsDueForCleanupAsync(utcNow, boundedLimit, cancellationToken);
    }

    public async Task MarkMediaUploadDeletedAsync(Guid uploadId, CancellationToken cancellationToken)
    {
        if (uploadId == Guid.Empty) throw new ArgumentException("Upload id is required.", nameof(uploadId));
        await _postStore.MarkMediaUploadDeletedAsync(uploadId, cancellationToken);
    }
}
