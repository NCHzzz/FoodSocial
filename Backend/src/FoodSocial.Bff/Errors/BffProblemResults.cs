using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Bff;

/// <summary>
/// Internal helper that builds the canonical ProblemDetails results used by the BFF
/// for identity session, moderation, and orchestration failures.
/// </summary>
internal static class BffProblemResults
{
    public static IResult IdentityOrchestrationFailed(HttpContext httpContext) =>
        Results.Problem(new ProblemDetails
        {
            Type = BffProblemTypes.IdentityOrchestrationFailed,
            Title = "Identity orchestration failed",
            Detail = "The BFF could not complete the identity orchestration request.",
            Status = StatusCodes.Status500InternalServerError,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        });

    public static IResult IdentitySessionForbidden(HttpContext httpContext) =>
        Results.Problem(new ProblemDetails
        {
            Type = BffProblemTypes.IdentitySessionForbidden,
            Title = "Identity session is not allowed",
            Detail = "The current identity state is not allowed to use this session.",
            Status = StatusCodes.Status403Forbidden,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        });

    public static IResult PostModerationForbidden(HttpContext httpContext) =>
        Results.Problem(new ProblemDetails
        {
            Type = BffProblemTypes.PostModerationForbidden,
            Title = "Post moderation role required",
            Detail = "A moderator or admin role is required for Post report review.",
            Status = StatusCodes.Status403Forbidden,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        });

    public static IResult PostServiceUnavailable(HttpContext httpContext) =>
        Results.Problem(new ProblemDetails
        {
            Type = BffProblemTypes.PostServiceUnavailable,
            Title = "Post service unavailable",
            Detail = "The BFF could not reach the Post service. Please retry later.",
            Status = StatusCodes.Status502BadGateway,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        });

    public static IResult UserProfileServiceUnavailable(HttpContext httpContext) =>
        Results.Problem(new ProblemDetails
        {
            Type = BffProblemTypes.UserProfileServiceUnavailable,
            Title = "UserProfile service unavailable",
            Detail = "The BFF could not reach the UserProfile service. Please retry later.",
            Status = StatusCodes.Status502BadGateway,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        });

    public static IResult InvalidRequest(HttpContext httpContext, string detail) =>
        Results.Problem(new ProblemDetails
        {
            Type = BffProblemTypes.InvalidPostRequest,
            Title = "Invalid Post request",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        });

    public static IResult InvalidReturnUrl(HttpContext httpContext) =>
        Results.BadRequest(new ProblemDetails
        {
            Type = BffProblemTypes.InvalidReturnUrl,
            Title = "Invalid return URL",
            Detail = "The return URL must be a relative path within the BFF.",
            Status = StatusCodes.Status400BadRequest,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        });
}
