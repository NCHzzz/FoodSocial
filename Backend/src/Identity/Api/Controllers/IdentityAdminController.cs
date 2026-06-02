using FoodSocial.Identity.Api.Authorization;
using FoodSocial.Identity.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Identity.Api.Controllers;

/// <summary>
/// Exposes account-level administrative role management endpoints.
/// </summary>
[ApiController]
[Route("identity/admin")]
[Authorize(Policy = IdentityAuthorizationPolicies.AccountAdminOnly)]
public sealed class IdentityAdminController(AssignIdentityRoleCommandHandler assignIdentityRoleCommandHandler) : ControllerBase
{
    private readonly AssignIdentityRoleCommandHandler _assignIdentityRoleCommandHandler = assignIdentityRoleCommandHandler ?? throw new ArgumentNullException(nameof(assignIdentityRoleCommandHandler));

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

        try
        {
            var result = await _assignIdentityRoleCommandHandler.HandleAsync(
                new AssignIdentityRoleCommand(request.Subject, request.Role),
                cancellationToken);

            return Ok(new AssignRoleResponse(result.Subject, result.Role, result.Created));
        }
        catch (ArgumentOutOfRangeException)
        {
            return InvalidRoleAssignmentRequest("The requested role is not supported.");
        }
    }

    private ActionResult InvalidRoleAssignmentRequest(string detail) =>
        this.FoodSocialProblem(
            title: "Invalid role assignment request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);
}
