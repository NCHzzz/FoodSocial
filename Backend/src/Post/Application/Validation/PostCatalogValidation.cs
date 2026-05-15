namespace FoodSocial.Post.Application.Validation;

public interface IPostCatalogReferenceValidator
{
    Task<PostCatalogReferenceValidationResult> ValidateReferencesAsync(Guid dishId, Guid? placeId, CancellationToken cancellationToken);
}

public sealed record PostCatalogReferenceValidationResult(bool IsValid, string? Error)
{
    public static PostCatalogReferenceValidationResult Valid { get; } = new(true, null);

    public static PostCatalogReferenceValidationResult Invalid(string error) => new(false, error);
}
