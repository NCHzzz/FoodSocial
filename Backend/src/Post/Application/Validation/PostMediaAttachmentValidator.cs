using FoodSocial.Post.Domain;

namespace FoodSocial.Post.Application.Validation;

public sealed class PostMediaAttachmentValidator
{
    public string? ValidateLocalMediaUploadReferences(
        IReadOnlyList<PostMediaRecord> media,
        out IReadOnlyList<PostMediaUploadAttachmentRequest> localUploadAttachments)
    {
        var attachments = new Dictionary<string, PostMediaUploadAttachmentRequest>(StringComparer.Ordinal);
        foreach (var item in media)
        {
            var urlIsLocalUpload = IsPostMediaUploadUrl(item.Url);
            var thumbnailIsLocalUpload = item.ThumbnailUrl is not null && IsPostMediaUploadUrl(item.ThumbnailUrl);
            if (thumbnailIsLocalUpload && !urlIsLocalUpload)
            {
                localUploadAttachments = [];
                return "Local uploaded thumbnails must be paired with their original uploaded media URL.";
            }

            if (!urlIsLocalUpload)
            {
                continue;
            }

            if (!thumbnailIsLocalUpload)
            {
                localUploadAttachments = [];
                return "Local uploaded media must include its paired local thumbnail URL.";
            }

            var localThumbnailUrl = item.ThumbnailUrl!;
            if (attachments.TryGetValue(item.Url, out var existing)
                && !string.Equals(existing.ThumbnailUrl, localThumbnailUrl, StringComparison.Ordinal))
            {
                localUploadAttachments = [];
                return "Local uploaded media cannot be published with conflicting thumbnail references.";
            }

            attachments[item.Url] = new PostMediaUploadAttachmentRequest(item.Url, localThumbnailUrl);
        }

        localUploadAttachments = attachments.Values.ToArray();
        return null;
    }

    private static bool IsPostMediaUploadUrl(string url) =>
        url.StartsWith("/api/media/uploads/", StringComparison.Ordinal);
}
