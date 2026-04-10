using FoodSocial.Identity.Api.Authorization;
using FoodSocial.Identity.Api.Contracts;
using FoodSocial.Identity.Application;
using FoodSocial.Identity.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Identity.Api.Controllers;

/// <summary>
/// Exposes account-level administrative role management endpoints.
/// </summary>
[ApiController]
[Route("identity/admin")]
[Authorize(Policy = IdentityAuthorizationPolicies.AccountAdminOnly)]
public sealed class IdentityAdminController(IIdentityUserStore identityUserStore) : ControllerBase
{
    private readonly IIdentityUserStore _identityUserStore = identityUserStore ?? throw new ArgumentNullException(nameof(identityUserStore));

    /// <summary>
    /// Assigns an application-owned role to a subject.
    /// </summary>
    /// <param name="request">The role assignment request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The assignment result.</returns>
    [HttpPost("roles/assign")]
    [ProducesResponseType<AssignRoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignRoleResponse>> AssignRole(
        [FromBody] AssignRoleRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return InvalidRoleAssignmentRequest("A role assignment request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return InvalidRoleAssignmentRequest("The target subject is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            return InvalidRoleAssignmentRequest("The requested role is required.");
        }

        string normalizedRole;

        try
        {
            normalizedRole = IdentityAppRoles.Normalize(request.Role);
        }
        catch (ArgumentOutOfRangeException)
        {
            return InvalidRoleAssignmentRequest("The requested role is not supported.");
        }

        var created = await _identityUserStore.EnsureRoleAssignedAsync(request.Subject, normalizedRole, cancellationToken);
        await _identityUserStore.SaveChangesAsync(cancellationToken);

        return Ok(new AssignRoleResponse(request.Subject, normalizedRole, created));
    }

    private ActionResult InvalidRoleAssignmentRequest(string detail) =>
        Problem(
            title: "Invalid role assignment request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);
}
