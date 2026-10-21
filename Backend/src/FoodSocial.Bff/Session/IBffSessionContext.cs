using FoodSocial.Contracts.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace FoodSocial.Bff;

/// <summary>
/// Resolves the current BFF-authenticated identity (cookie or bearer) into a session
/// context with identity id, role set, and a short-circuiting IResult on failure.
/// </summary>
public interface IBffSessionContext
{
    /// <summary>
    /// Resolves the current allowed session. Returns <c>null</c> when access is granted and
    /// a non-null <see cref="IResult"/> when access must be denied.
    /// </summary>
    Task<ResolvedBffSession?> ResolveAllowedAsync(HttpContext httpContext, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves the current allowed session for moderator/admin-only routes.
    /// </summary>
    Task<ResolvedBffSession?> ResolveModeratorAsync(HttpContext httpContext, CancellationToken cancellationToken);
}

/// <summary>
/// Result envelope returned by <see cref="IBffSessionContext"/>. When <see cref="Result"/> is
/// non-null, the caller should short-circuit the request with that IResult.
/// </summary>
public sealed record ResolvedBffSession(Guid IdentityId, IReadOnlyList<string> Roles, IResult? Result)
{
    public static ResolvedBffSession Success(Guid identityId, IReadOnlyList<string> roles) => new(identityId, roles, null);

    public static ResolvedBffSession Unauthorized() => new(Guid.Empty, [], Results.Unauthorized());

    public static ResolvedBffSession Forbidden(HttpContext httpContext) => new(
        Guid.Empty,
        [],
        BffProblemResults.IdentitySessionForbidden(httpContext));

    public static ResolvedBffSession ModerationForbidden(HttpContext httpContext) => new(
        Guid.Empty,
        [],
        BffProblemResults.PostModerationForbidden(httpContext));

    public static ResolvedBffSession OrchestrationFailed(HttpContext httpContext) => new(
        Guid.Empty,
        [],
        BffProblemResults.IdentityOrchestrationFailed(httpContext));
}
