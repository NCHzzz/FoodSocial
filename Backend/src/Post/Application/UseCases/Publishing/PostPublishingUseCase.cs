using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application.Mapping;
using FoodSocial.Post.Application.Validation;
using FoodSocial.Post.Domain;

namespace FoodSocial.Post.Application.UseCases.Publishing;

public sealed class PostPublishingUseCase(
    IPostStore postStore,
    IPostCatalogReferenceValidator catalogReferenceValidator,
    PostResponseMapper mapper,
    PostMediaAttachmentValidator mediaAttachmentValidator)
{
    private readonly IPostStore _postStore = postStore ?? throw new ArgumentNullException(nameof(postStore));
    private readonly IPostCatalogReferenceValidator _catalogReferenceValidator = catalogReferenceValidator ?? throw new ArgumentNullException(nameof(catalogReferenceValidator));
    private readonly PostResponseMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly PostMediaAttachmentValidator _mediaAttachmentValidator = mediaAttachmentValidator ?? throw new ArgumentNullException(nameof(mediaAttachmentValidator));

    public async Task<PostMutationResult> CreatePostAsync(CreatePostRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var catalogValidation = await _catalogReferenceValidator.ValidateReferencesAsync(request.DishId, request.PlaceId, cancellationToken);
        if (!catalogValidation.IsValid)
        {
            return new PostMutationResult(PostOperationStatus.Invalid, null, catalogValidation.Error);
        }

        try
        {
            var media = request.Media.Select((item, index) => PostMediaRecord.Create(item.Url, item.Type, item.ThumbnailUrl, item.Caption, item.IsPrimary || index == 0, index)).ToArray();
            if (media.Length > 0 && media.All(item => !item.IsPrimary)) media[0] = PostMediaRecord.Create(media[0].Url, media[0].Type, media[0].ThumbnailUrl, media[0].Caption, true, media[0].SortOrder);
            var localUploadValidationError = _mediaAttachmentValidator.ValidateLocalMediaUploadReferences(media, out var localUploadAttachments);
            if (localUploadValidationError is not null)
            {
                return new PostMutationResult(PostOperationStatus.Invalid, null, localUploadValidationError);
            }

            if (localUploadAttachments.Count > 0)
            {
                var attachableUrls = await _postStore.FindAttachableMediaUploadUrlsAsync(request.AuthorId, localUploadAttachments, cancellationToken);
                var missingUrls = localUploadAttachments.Where(upload => !attachableUrls.Contains(upload.Url)).ToArray();
                if (missingUrls.Length > 0)
                {
                    return new PostMutationResult(PostOperationStatus.Invalid, null, "Uploaded media must exist, belong to the author, and be pending attachment before publish.");
                }
            }

            var post = PostRecord.Create(request.AuthorId, request.DishId, request.Caption, media, request.PlaceId, request.Rating, request.PriceRange, request.Tags, request.LocationText);
            var created = localUploadAttachments.Count > 0
                ? await _postStore.CreatePostWithMediaUploadsAsync(post, localUploadAttachments, cancellationToken)
                : await _postStore.CreatePostAsync(post, cancellationToken);
            return new PostMutationResult(PostOperationStatus.Created, _mapper.MapDetail(created), null);
        }
        catch (ArgumentException exception)
        {
            return new PostMutationResult(PostOperationStatus.Invalid, null, exception.Message);
        }
        catch (PostMediaUploadAttachmentException exception)
        {
            return new PostMutationResult(PostOperationStatus.Invalid, null, exception.Message);
        }
    }
}
