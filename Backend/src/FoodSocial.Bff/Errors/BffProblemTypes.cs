namespace FoodSocial.Bff;

/// <summary>
/// Canonical problem-type URIs surfaced by the BFF. Centralized so renames stay in sync
/// across endpoint files and the documented error catalog.
/// </summary>
public static class BffProblemTypes
{
    public const string InvalidReturnUrl = "https://foodsocial/errors/bff/invalid-return-url";
    public const string IdentityOrchestrationFailed = "https://foodsocial/errors/bff/identity-orchestration-failed";
    public const string IdentitySessionForbidden = "https://foodsocial/errors/bff/identity-session-forbidden";
    public const string PostServiceUnavailable = "https://foodsocial/errors/bff/post-service-unavailable";
    public const string UserProfileServiceUnavailable = "https://foodsocial/errors/bff/user-profile-service-unavailable";
    public const string InvalidPostRequest = "https://foodsocial/errors/bff/invalid-post-request";
    public const string PostModerationForbidden = "https://foodsocial/errors/bff/post-moderation-forbidden";
}
