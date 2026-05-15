namespace FoodSocial.Post.Api.Media;

public sealed class PostMediaUploadValidator
{
    private static readonly IReadOnlyDictionary<string, string> AllowedImageContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    public async Task<PostMediaUploadValidationResult> ValidateImageUploadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length <= 0 || string.IsNullOrWhiteSpace(file.FileName))
        {
            return PostMediaUploadValidationResult.Invalid("A non-empty media file is required.", "empty_file");
        }

        if (file.Length > 10_000_000)
        {
            return PostMediaUploadValidationResult.Invalid("Media uploads must be 10 MB or smaller.", "file_too_large");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageContentTypes.TryGetValue(extension, out var expectedContentType)
            || !string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return PostMediaUploadValidationResult.Invalid("Only JPEG, PNG, and WebP image uploads are supported in the MVP media pipeline.", "unsupported_type");
        }

        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header, cancellationToken);
        if (!HasExpectedImageSignature(extension, header.AsSpan(0, bytesRead)))
        {
            return PostMediaUploadValidationResult.Invalid("Uploaded media bytes do not match the declared image type.", "signature_mismatch");
        }

        return PostMediaUploadValidationResult.Valid(extension, expectedContentType);
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
}

public sealed record PostMediaUploadValidationResult(string? Extension, string? ContentType, string? Error, string? Reason)
{
    internal static PostMediaUploadValidationResult Valid(string extension, string contentType) => new(extension, contentType, null, null);

    internal static PostMediaUploadValidationResult Invalid(string error, string reason = "invalid_file") => new(null, null, error, reason);
}
