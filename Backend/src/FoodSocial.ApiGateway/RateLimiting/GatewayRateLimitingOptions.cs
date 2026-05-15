namespace FoodSocial.ApiGateway;

internal sealed class GatewayRateLimitingOptions
{
    public const string SectionName = "Gateway:RateLimiting";

    public bool Enabled { get; set; } = true;
    public GatewayRateLimitPolicyOptions Default { get; set; } = new() { PermitLimit = 300, WindowSeconds = 60 };
    public GatewayRateLimitPolicyOptions Auth { get; set; } = new() { PermitLimit = 30, WindowSeconds = 60 };
    public GatewayRateLimitPolicyOptions RiskyWrite { get; set; } = new() { PermitLimit = 60, WindowSeconds = 60 };
    public GatewayRateLimitPolicyOptions MediaUpload { get; set; } = new() { PermitLimit = 20, WindowSeconds = 60 };
}

internal sealed class GatewayRateLimitPolicyOptions
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}
