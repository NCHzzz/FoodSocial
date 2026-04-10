namespace FoodSocial.Contracts.Identity;

/// <summary>
/// Represents the deterministic BFF auth-edge response for session refresh or renewal attempts.
/// </summary>
/// <param name="Refreshed"><c>true</c> when a real refresh/renewal operation occurred; otherwise <c>false</c>.</param>
/// <param name="SessionValid"><c>true</c> when the current authenticated session remains valid.</param>
/// <param name="ReauthenticationRequired"><c>true</c> when the caller must trigger a fresh interactive sign-in.</param>
/// <param name="Reason">A stable, explicit reason describing the refresh outcome.</param>
/// <param name="ServerTimeUtc">The UTC server timestamp when the response was produced.</param>
public sealed record RefreshSessionResponse(
    bool Refreshed,
    bool SessionValid,
    bool ReauthenticationRequired,
    string Reason,
    DateTimeOffset ServerTimeUtc);
