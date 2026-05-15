using Microsoft.AspNetCore.StaticFiles;

namespace FoodSocial.Post.Api.Media;

public sealed class PostMediaStorage(IConfiguration configuration, IWebHostEnvironment environment)
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IWebHostEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public async Task<StoredPostMediaUpload> StoreImageWithDerivativeAsync(Guid uploadId, IFormFile file, string extension, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (uploadId == Guid.Empty) throw new ArgumentException("Upload id is required.", nameof(uploadId));
        if (string.IsNullOrWhiteSpace(extension)) throw new ArgumentException("Media extension is required.", nameof(extension));

        var fileName = $"{uploadId:N}{extension}";
        var originalPath = ResolvePath("originals", fileName);
        var thumbnailPath = ResolvePath("thumbnails", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(originalPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);

        await using (var stream = File.Create(originalPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        File.Copy(originalPath, thumbnailPath, overwrite: true);
        return new StoredPostMediaUpload(
            $"/api/media/uploads/{uploadId}/original",
            $"/api/media/uploads/{uploadId}/thumbnail",
            file.Length);
    }

    public StoredPostMediaFile? Find(Guid uploadId, string variant)
    {
        if (uploadId == Guid.Empty) return null;
        var directoryName = string.Equals(variant, "thumbnail", StringComparison.OrdinalIgnoreCase) ? "thumbnails" : "originals";
        var directory = ResolveDirectory(directoryName);
        if (!Directory.Exists(directory)) return null;
        var path = Directory.EnumerateFiles(directory, $"{uploadId:N}.*").FirstOrDefault();
        if (path is null) return null;
        return new StoredPostMediaFile(path, ResolveContentType(path));
    }

    public Task DeleteUploadAsync(Guid uploadId)
    {
        if (uploadId == Guid.Empty) return Task.CompletedTask;
        DeleteVariantFiles("originals", uploadId);
        DeleteVariantFiles("thumbnails", uploadId);
        return Task.CompletedTask;
    }

    private void DeleteVariantFiles(string directoryName, Guid uploadId)
    {
        var directory = ResolveDirectory(directoryName);
        if (!Directory.Exists(directory)) return;
        foreach (var path in Directory.EnumerateFiles(directory, $"{uploadId:N}.*"))
        {
            File.Delete(path);
        }
    }

    private string ResolvePath(string directoryName, string fileName) => Path.Combine(ResolveDirectory(directoryName), fileName);

    private string ResolveDirectory(string directoryName) => Path.Combine(ResolveRoot(), directoryName);

    private string ResolveRoot()
    {
        var root = _configuration["Post:Media:StoragePath"];
        return string.IsNullOrWhiteSpace(root) ? Path.Combine(_environment.ContentRootPath, "App_Data", "media") : root;
    }

    private string ResolveContentType(string path) =>
        _contentTypeProvider.TryGetContentType(path, out var contentType) ? contentType : "application/octet-stream";
}

public sealed record StoredPostMediaUpload(string OriginalUrl, string ThumbnailUrl, long SizeBytes);
public sealed record StoredPostMediaFile(string Path, string ContentType);
