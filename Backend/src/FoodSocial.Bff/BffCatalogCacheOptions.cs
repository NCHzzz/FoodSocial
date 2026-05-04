namespace FoodSocial.Bff;

internal sealed class BffCatalogCacheOptions
{
    public bool Enabled { get; init; } = true;

    public int SuccessfulResponseTtlSeconds { get; init; } = 60;
}
