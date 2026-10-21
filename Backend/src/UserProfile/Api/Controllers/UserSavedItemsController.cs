using System.Security.Claims;
using FoodSocial.Contracts.Identity;
using FoodSocial.Contracts.UserProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.UserProfile.Api.Controllers;

/// <summary>
/// Exposes the application-owned saved-item endpoints (posts, dishes, places).
/// </summary>
[ApiController]
[Route("api/users")]
public sealed class UserSavedItemsController(UserSavedItemsService userSavedItemsService) : ControllerBase
{
    private readonly UserSavedItemsService _userSavedItemsService = userSavedItemsService ?? throw new ArgumentNullException(nameof(userSavedItemsService));

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

    private static UserSavedItemsResponse MapSavedItems(UserSavedItemsSnapshot snapshot) =>
        new(
            snapshot.Posts.Select(item => new SavedPostReferenceDto(item.PostId, item.SavedAtUtc)).ToArray(),
            snapshot.Dishes.Select(item => new SavedDishReferenceDto(item.DishId, item.SavedAtUtc)).ToArray(),
            snapshot.Places.Select(item => new SavedPlaceReferenceDto(item.PlaceId, item.SavedAtUtc)).ToArray());

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
