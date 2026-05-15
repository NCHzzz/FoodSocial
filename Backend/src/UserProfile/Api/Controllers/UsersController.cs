using System.Security.Claims;
using FoodSocial.Contracts.Identity;
using FoodSocial.Contracts.UserProfiles;
using FoodSocial.UserProfile.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.UserProfile.Api.Controllers;

/// <summary>
/// Exposes the application-owned user profile graph endpoints.
/// </summary>
[ApiController]
[Route("api/users")]
public sealed class UsersController(
    UserProfileService userProfileService,
    UserFollowService userFollowService,
    UserSavedItemsService userSavedItemsService,
    UserNotificationService userNotificationService)
    : ControllerBase
{
    private readonly UserProfileService _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
    private readonly UserFollowService _userFollowService = userFollowService ?? throw new ArgumentNullException(nameof(userFollowService));
    private readonly UserSavedItemsService _userSavedItemsService = userSavedItemsService ?? throw new ArgumentNullException(nameof(userSavedItemsService));
    private readonly UserNotificationService _userNotificationService = userNotificationService ?? throw new ArgumentNullException(nameof(userNotificationService));

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

    [HttpGet("{userId:guid}/saved")]
    [Authorize]
    [ProducesResponseType<UserSavedItemsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSavedItemsResponse>> GetSavedItems(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedSubject(User, out var subject, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var result = await _userSavedItemsService.GetSavedItemsAsync(subject!, userId, cancellationToken);
        return result.Status switch
        {
            UserProfileOperationStatus.Success => Ok(MapSavedItems(result.SavedItems!)),
            UserProfileOperationStatus.Forbidden => Problem(title: "Profile ownership required", detail: result.Error, statusCode: StatusCodes.Status403Forbidden),
            UserProfileOperationStatus.NotFound => Problem(title: "User profile not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Saved items lookup failed", detail: result.Error ?? "The saved items could not be retrieved.", statusCode: StatusCodes.Status400BadRequest)
        };
    }

    [HttpPut("{userId:guid}/saved/posts/{postId:guid}")]
    [Authorize]
    [ProducesResponseType<SaveUserItemResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<SaveUserItemResponse>> SavePost(Guid userId, Guid postId, CancellationToken cancellationToken) =>
        SaveItemAsync(userId, postId, (subject, identityId, targetId, token) => _userSavedItemsService.SavePostAsync(subject, identityId, targetId, token), cancellationToken);

    [HttpDelete("{userId:guid}/saved/posts/{postId:guid}")]
    [Authorize]
    [ProducesResponseType<SaveUserItemResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<SaveUserItemResponse>> UnsavePost(Guid userId, Guid postId, CancellationToken cancellationToken) =>
        SaveItemAsync(userId, postId, (subject, identityId, targetId, token) => _userSavedItemsService.UnsavePostAsync(subject, identityId, targetId, token), cancellationToken);

    [HttpPut("{userId:guid}/saved/dishes/{dishId:guid}")]
    [Authorize]
    [ProducesResponseType<SaveUserItemResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<SaveUserItemResponse>> SaveDish(Guid userId, Guid dishId, CancellationToken cancellationToken) =>
        SaveItemAsync(userId, dishId, (subject, identityId, targetId, token) => _userSavedItemsService.SaveDishAsync(subject, identityId, targetId, token), cancellationToken);

    [HttpDelete("{userId:guid}/saved/dishes/{dishId:guid}")]
    [Authorize]
    [ProducesResponseType<SaveUserItemResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<SaveUserItemResponse>> UnsaveDish(Guid userId, Guid dishId, CancellationToken cancellationToken) =>
        SaveItemAsync(userId, dishId, (subject, identityId, targetId, token) => _userSavedItemsService.UnsaveDishAsync(subject, identityId, targetId, token), cancellationToken);

    [HttpPut("{userId:guid}/saved/places/{placeId:guid}")]
    [Authorize]
    [ProducesResponseType<SaveUserItemResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<SaveUserItemResponse>> SavePlace(Guid userId, Guid placeId, CancellationToken cancellationToken) =>
        SaveItemAsync(userId, placeId, (subject, identityId, targetId, token) => _userSavedItemsService.SavePlaceAsync(subject, identityId, targetId, token), cancellationToken);

    [HttpDelete("{userId:guid}/saved/places/{placeId:guid}")]
    [Authorize]
    [ProducesResponseType<SaveUserItemResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<SaveUserItemResponse>> UnsavePlace(Guid userId, Guid placeId, CancellationToken cancellationToken) =>
        SaveItemAsync(userId, placeId, (subject, identityId, targetId, token) => _userSavedItemsService.UnsavePlaceAsync(subject, identityId, targetId, token), cancellationToken);

    [HttpGet("{userId:guid}/notifications")]
    [Authorize]
    [ProducesResponseType<UserNotificationListResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserNotificationListResponse>> GetNotifications(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedSubject(User, out var subject, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var snapshot = await _userNotificationService.GetNotificationsAsync(subject!, userId, cancellationToken);
        return snapshot is null
            ? Problem(title: "Notification ownership required", detail: "The authenticated user does not own the requested notification stream.", statusCode: StatusCodes.Status403Forbidden)
            : Ok(new UserNotificationListResponse(snapshot.Items.Select(MapNotification).ToArray(), snapshot.Limit, snapshot.UnreadCount));
    }

    [HttpPatch("{userId:guid}/notifications/{notificationId:guid}/read")]
    [Authorize]
    [ProducesResponseType<UserNotificationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserNotificationResponse>> MarkNotificationRead(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedSubject(User, out var subject, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var snapshot = await _userNotificationService.MarkNotificationReadAsync(subject!, userId, notificationId, cancellationToken);
        return snapshot is null
            ? Problem(title: "Notification not found", detail: "The requested notification was not found for the authenticated user.", statusCode: StatusCodes.Status404NotFound)
            : Ok(MapNotification(snapshot));
    }

    private async Task<ActionResult<SaveUserItemResponse>> SaveItemAsync(
        Guid userId,
        Guid targetId,
        Func<string, Guid, Guid, CancellationToken, Task<UserSavedItemMutationResult>> operation,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedSubject(User, out var subject, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var result = await operation(subject!, userId, targetId, cancellationToken);
        return result.Status switch
        {
            UserProfileOperationStatus.Success => Ok(new SaveUserItemResponse(result.OwnerIdentityId, result.TargetId, result.TargetType, result.Saved)),
            UserProfileOperationStatus.Forbidden => Problem(title: "Profile ownership required", detail: result.Error, statusCode: StatusCodes.Status403Forbidden),
            UserProfileOperationStatus.NotFound => Problem(title: "User profile not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Saved-item operation failed", detail: result.Error ?? "The saved-item operation could not be completed.", statusCode: StatusCodes.Status400BadRequest)
        };
    }

    private ActionResult<FollowUserResponse> MapMutationResult(UserProfileMutationResult result) =>
        result.Status switch
        {
            UserProfileOperationStatus.Success => Ok(new FollowUserResponse(result.TargetIdentityId, result.Following)),
            UserProfileOperationStatus.NotFound => Problem(title: "User profile not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            UserProfileOperationStatus.Invalid => Problem(title: "Invalid follow request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest),
            _ => Problem(title: "Follow operation failed", detail: result.Error ?? "The follow operation could not be completed.", statusCode: StatusCodes.Status400BadRequest)
        };

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

    private static UserProfileListResponse MapConnections(UserProfileConnectionsSnapshot snapshot) =>
        new(
            snapshot.Items.Select(item => new UserProfileSummaryDto(item.IdentityId, item.DisplayName, item.AvatarUrl)).ToArray(),
            snapshot.TotalCount);

    private static UserSavedItemsResponse MapSavedItems(UserSavedItemsSnapshot snapshot) =>
        new(
            snapshot.Posts.Select(item => new SavedPostReferenceDto(item.PostId, item.SavedAtUtc)).ToArray(),
            snapshot.Dishes.Select(item => new SavedDishReferenceDto(item.DishId, item.SavedAtUtc)).ToArray(),
            snapshot.Places.Select(item => new SavedPlaceReferenceDto(item.PlaceId, item.SavedAtUtc)).ToArray());

    private static UserNotificationResponse MapNotification(UserNotificationSnapshot snapshot) =>
        new(snapshot.Id, snapshot.RecipientIdentityId, snapshot.ActorIdentityId, snapshot.Type, snapshot.Message, snapshot.IsRead, snapshot.CreatedAtUtc, snapshot.ReadAtUtc);

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
