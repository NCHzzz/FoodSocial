namespace FoodSocial.Bff;

internal sealed class BffRateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; init; } = true;
    public BffRateLimitPolicyOptions Default { get; init; } = new(PermitLimit: 300, WindowSeconds: 60);
    public BffRateLimitPolicyOptions Auth { get; init; } = new(PermitLimit: 60, WindowSeconds: 60);
    public BffRateLimitPolicyOptions RiskyWrite { get; init; } = new(PermitLimit: 30, WindowSeconds: 60);
    public BffRateLimitPolicyOptions MediaUpload { get; init; } = new(PermitLimit: 10, WindowSeconds: 60);
    public BffRateLimitPolicyOptions SearchAssist { get; init; } = new(PermitLimit: 60, WindowSeconds: 60);
}

internal sealed record BffRateLimitPolicyOptions(int PermitLimit, int WindowSeconds);
