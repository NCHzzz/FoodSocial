namespace FoodSocial.Bff;

internal sealed record CachedBffResponse(int StatusCode, string? ContentType, byte[] Body);
