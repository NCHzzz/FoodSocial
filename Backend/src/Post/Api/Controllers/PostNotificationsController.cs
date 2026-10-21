using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application.UseCases.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Post.Api.Controllers;

/// <summary>
/// Exposes the Post-owned notification endpoints (list, mark read).
/// </summary>
[ApiController]
public sealed class PostNotificationsController(PostNotificationUseCase notificationUseCase) : ControllerBase
{
    private readonly PostNotificationUseCase _notificationUseCase = notificationUseCase ?? throw new ArgumentNullException(nameof(notificationUseCase));

    [HttpGet("api/notifications")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<NotificationListResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationListResponse>> GetNotifications(
        [FromQuery] int? limit,
        CancellationToken cancellationToken) =>
        Ok(await _notificationUseCase.GetNotificationsAsync(PostInternalAuthorization.GetIdentityId(HttpContext), limit, cancellationToken));

    [HttpPatch("api/notifications/{notificationId:guid}/read")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<NotificationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationResponse>> MarkNotificationRead(Guid notificationId, CancellationToken cancellationToken)
    {
        var result = await _notificationUseCase.MarkNotificationReadAsync(notificationId, PostInternalAuthorization.GetIdentityId(HttpContext), cancellationToken);
        return result.Status switch
        {
            PostOperationStatus.Updated => Ok(result.Notification),
            PostOperationStatus.NotFound => Problem(title: "Notification not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Invalid notification request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest)
        };
    }
}
