using System.Security.Claims;
using FoodSocial.Contracts.Identity;
using FoodSocial.Contracts.UserProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.UserProfile.Api.Controllers;

/// <summary>
/// Exposes the application-owned user profile read/update endpoints.
/// </summary>
[ApiController]
[Route("api/users")]
public sealed class UserProfilesController(UserProfileService userProfileService) : ControllerBase
{
    private readonly UserProfileService _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));

    [HttpGet("{userId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetProfile(Guid userId, CancellationToken cancellationToken)
    {
        var snapshot = await _userProfileService.GetProfileAsync(userId, cancellationToken);
        return snapshot is null
            ? Problem(title: "User profile not found", detail: "The requested user profile was not found.", statusCode: StatusCodes.Status404NotFound)
            : Ok(MapProfile(snapshot));
    }

    [HttpPatch("{userId:guid}")]
    [Authorize]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile(
        Guid userId,
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedSubject(User, out var subject, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var result = await _userProfileService.UpdateProfileAsync(subject!, userId, MapCommand(request), cancellationToken);
        return result.Status switch
        {
            UserProfileOperationStatus.Success => Ok(MapProfile(result.Profile!)),
            UserProfileOperationStatus.Forbidden => Problem(title: "Profile ownership required", detail: result.Error, statusCode: StatusCodes.Status403Forbidden),
            UserProfileOperationStatus.NotFound => Problem(title: "User profile not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Profile update failed", detail: result.Error ?? "The profile update could not be completed.", statusCode: StatusCodes.Status400BadRequest)
        };
    }

    private static UserProfileResponse MapProfile(UserProfileSnapshot snapshot) =>
        new(
            snapshot.IdentityId,
            snapshot.DisplayName,
            snapshot.AvatarUrl,
            snapshot.Bio,
            snapshot.PreferredCuisines,
            snapshot.DislikedIngredients,
            snapshot.DietaryRestrictions,
            snapshot.FollowerCount,
            snapshot.FollowingCount);

    private static UpdateUserProfileCommand MapCommand(UpdateUserProfileRequest request) =>
        new(
            request.DisplayName,
            request.AvatarUrl,
            request.Bio,
            request.PreferredCuisines ?? [],
            request.DislikedIngredients ?? [],
            request.DietaryRestrictions ?? []);

    private ActionResult ProblemMissingSubject() =>
        Problem(
            title: "Missing subject claim",
            detail: "The authenticated token does not contain a subject claim.",
            statusCode: StatusCodes.Status401Unauthorized);

    private bool TryGetAuthenticatedSubject(ClaimsPrincipal user, out string? subject, out ActionResult? failureResult)
    {
        subject = IdentityClaimReader.GetSubject(user);
        if (string.IsNullOrWhiteSpace(subject))
        {
            failureResult = ProblemMissingSubject();
            return false;
        }

        failureResult = null;
        return true;
    }
}
