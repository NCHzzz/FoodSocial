namespace FoodSocial.Bff;

internal sealed class BffCatalogCacheOptions
{
    public const int MinimumSuccessfulResponseTtlSeconds = 1;

    public const int MaximumSuccessfulResponseTtlSeconds = 300;

    public bool Enabled { get; init; } = true;

    public int SuccessfulResponseTtlSeconds { get; init; } = 60;

    public int MaxBodyBytes { get; init; } = 64 * 1024;
}
