using System.Security.Claims;
using FoodSocial.Contracts.Identity;
using FoodSocial.Contracts.UserProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.UserProfile.Api.Controllers;

/// <summary>
/// Exposes the user-owned notification read endpoints.
/// </summary>
[ApiController]
[Route("api/users")]
public sealed class UserNotificationsController(UserNotificationService userNotificationService) : ControllerBase
{
    private readonly UserNotificationService _userNotificationService = userNotificationService ?? throw new ArgumentNullException(nameof(userNotificationService));

    [HttpGet("{userId:guid}/notifications")]
    [Authorize]
    [ProducesResponseType<UserNotificationListResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserNotificationListResponse>> GetNotifications(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedSubject(User, out var subject, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var result = await _userNotificationService.GetNotificationsAsync(subject!, userId, cancellationToken);
        return result.Status switch
        {
            UserProfileOperationStatus.Success => Ok(new UserNotificationListResponse(result.Notifications!.Items.Select(MapNotification).ToArray(), result.Notifications.Limit, result.Notifications.UnreadCount)),
            UserProfileOperationStatus.Forbidden => Problem(title: "Notification ownership required", detail: result.Error, statusCode: StatusCodes.Status403Forbidden),
            UserProfileOperationStatus.NotFound => Problem(title: "User profile not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Notification lookup failed", detail: result.Error ?? "The notifications could not be retrieved.", statusCode: StatusCodes.Status400BadRequest)
        };
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

        var result = await _userNotificationService.MarkNotificationReadAsync(subject!, userId, notificationId, cancellationToken);
        return result.Status switch
        {
            UserProfileOperationStatus.Success => Ok(MapNotification(result.Notification!)),
            UserProfileOperationStatus.Forbidden => Problem(title: "Notification ownership required", detail: result.Error, statusCode: StatusCodes.Status403Forbidden),
            UserProfileOperationStatus.NotFound => Problem(title: "Notification not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Notification update failed", detail: result.Error ?? "The notification could not be updated.", statusCode: StatusCodes.Status400BadRequest)
        };
    }

    private static UserNotificationResponse MapNotification(UserNotificationSnapshot snapshot) =>
        new(snapshot.Id, snapshot.RecipientIdentityId, snapshot.ActorIdentityId, snapshot.Type, snapshot.Message, snapshot.IsRead, snapshot.CreatedAtUtc, snapshot.ReadAtUtc);

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
