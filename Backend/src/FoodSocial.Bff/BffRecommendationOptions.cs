namespace FoodSocial.Bff;

internal sealed class BffRecommendationOptions
{
    public const string SectionName = "Recommendations";

    public bool AiEnabled { get; init; }
    public int DefaultLimit { get; init; } = 10;
    public int MaxLimit { get; init; } = 25;
    public int ProviderTimeoutMilliseconds { get; init; } = 750;
}
