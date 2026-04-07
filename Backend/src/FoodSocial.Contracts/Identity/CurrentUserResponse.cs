namespace FoodSocial.Contracts.Identity;

public sealed record CurrentUserResponse(
    AuthenticatedUserDto User,
    DateTimeOffset ServerTimeUtc);
