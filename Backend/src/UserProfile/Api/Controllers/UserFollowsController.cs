using System.Security.Claims;
using FoodSocial.Contracts.Identity;
using FoodSocial.Contracts.UserProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.UserProfile.Api.Controllers;

/// <summary>
/// Exposes the application-owned follow graph and safety relationship endpoints
/// (follow, unfollow, followers, following, block, unblock, mute, unmute).
/// </summary>
[ApiController]
[Route("api/users")]
public sealed class UserFollowsController(
    UserFollowService userFollowService,
    UserSafetyService userSafetyService) : ControllerBase
{
    private readonly UserFollowService _userFollowService = userFollowService ?? throw new ArgumentNullException(nameof(userFollowService));
    private readonly UserSafetyService _userSafetyService = userSafetyService ?? throw new ArgumentNullException(nameof(userSafetyService));

    [HttpPost("{userId:guid}/follow")]
    [Authorize]
    [ProducesResponseType<FollowUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FollowUserResponse>> Follow(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedSubject(User, out var subject, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var result = await _userFollowService.FollowAsync(subject!, userId, cancellationToken);
        return MapMutationResult(result);
    }

    [HttpDelete("{userId:guid}/follow")]
    [Authorize]
    [ProducesResponseType<FollowUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FollowUserResponse>> Unfollow(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedSubject(User, out var subject, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var result = await _userFollowService.UnfollowAsync(subject!, userId, cancellationToken);
        return MapMutationResult(result);
    }

    [HttpPost("{userId:guid}/block")]
    [Authorize]
    [ProducesResponseType<UserSafetyMutationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSafetyMutationResponse>> Block(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedSubject(User, out var subject, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var result = await _userSafetyService.BlockAsync(subject!, userId, cancellationToken);
        return MapSafetyResult(result);
    }

    [HttpDelete("{userId:guid}/block")]
    [Authorize]
    [ProducesResponseType<UserSafetyMutationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSafetyMutationResponse>> Unblock(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedSubject(User, out var subject, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var result = await _userSafetyService.UnblockAsync(subject!, userId, cancellationToken);
        return MapSafetyResult(result);
    }

    [HttpPost("{userId:guid}/mute")]
    [Authorize]
    [ProducesResponseType<UserSafetyMutationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSafetyMutationResponse>> Mute(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedSubject(User, out var subject, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var result = await _userSafetyService.MuteAsync(subject!, userId, cancellationToken);
        return MapSafetyResult(result);
    }

    [HttpDelete("{userId:guid}/mute")]
    [Authorize]
    [ProducesResponseType<UserSafetyMutationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSafetyMutationResponse>> Unmute(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedSubject(User, out var subject, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var result = await _userSafetyService.UnmuteAsync(subject!, userId, cancellationToken);
        return MapSafetyResult(result);
    }

    [HttpGet("{userId:guid}/followers")]
    [AllowAnonymous]
    [ProducesResponseType<UserProfileListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileListResponse>> GetFollowers(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _userFollowService.GetFollowersAsync(userId, cancellationToken);
        return result is null
            ? Problem(title: "User profile not found", detail: "The requested user profile was not found.", statusCode: StatusCodes.Status404NotFound)
            : Ok(MapConnections(result));
    }

    [HttpGet("{userId:guid}/following")]
    [AllowAnonymous]
    [ProducesResponseType<UserProfileListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileListResponse>> GetFollowing(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _userFollowService.GetFollowingAsync(userId, cancellationToken);
        return result is null
            ? Problem(title: "User profile not found", detail: "The requested user profile was not found.", statusCode: StatusCodes.Status404NotFound)
            : Ok(MapConnections(result));
    }

    private static UserProfileListResponse MapConnections(UserProfileConnectionsSnapshot snapshot) =>
        new(
            snapshot.Items.Select(item => new UserProfileSummaryDto(item.IdentityId, item.DisplayName, item.AvatarUrl)).ToArray(),
            snapshot.TotalCount);

    private ActionResult<FollowUserResponse> MapMutationResult(UserProfileMutationResult result) =>
        result.Status switch
        {
            UserProfileOperationStatus.Success => Ok(new FollowUserResponse(result.TargetIdentityId, result.Following)),
            UserProfileOperationStatus.NotFound => Problem(title: "User profile not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            UserProfileOperationStatus.Invalid => Problem(title: "Invalid follow request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest),
            _ => Problem(title: "Follow operation failed", detail: result.Error ?? "The follow operation could not be completed.", statusCode: StatusCodes.Status400BadRequest)
        };

    private ActionResult<UserSafetyMutationResponse> MapSafetyResult(UserSafetyMutationResult result) =>
        result.Status switch
        {
            UserProfileOperationStatus.Success => Ok(new UserSafetyMutationResponse(result.TargetIdentityId, result.Type.ToString(), result.IsActive)),
            UserProfileOperationStatus.Invalid => Problem(title: "Invalid request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest),
            _ => Problem(title: "Safety operation failed", detail: result.Error ?? "The operation could not be completed.", statusCode: StatusCodes.Status400BadRequest)
        };

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
