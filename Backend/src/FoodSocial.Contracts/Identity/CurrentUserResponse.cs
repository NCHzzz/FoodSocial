namespace FoodSocial.Contracts.Identity;

/// <summary>
/// Represents the composite authenticated session payload returned by the BFF auth edge.
/// </summary>
/// <param name="User">The authenticated user view for the current session.</param>
/// <param name="Identity">The local identity/bootstrap state associated with the session.</param>
/// <param name="ServerTimeUtc">The UTC server timestamp when the response was produced.</param>
public sealed record CurrentUserResponse(
    AuthenticatedUserDto User,
    SessionIdentityDto Identity,
    DateTimeOffset ServerTimeUtc);
