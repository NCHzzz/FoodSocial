using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc;

public static class FoodSocialProblemDetails
{
    private const string TraceIdExtensionName = "traceId";
    private const string ProblemTypeBaseUri = "https://foodsocial/errors/common";

    public static ProblemDetails Create(
        HttpContext httpContext,
        int statusCode,
        string title,
        string? detail,
        string? type = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new ProblemDetails
        {
            Type = string.IsNullOrWhiteSpace(type) ? CreateDefaultType(statusCode) : type,
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                [TraceIdExtensionName] = httpContext.TraceIdentifier
            }
        };
    }

    public static ObjectResult FoodSocialProblem(
        this ControllerBase controller,
        string title,
        string? detail,
        int statusCode,
        string? type = null)
    {
        ArgumentNullException.ThrowIfNull(controller);

        var problemDetails = Create(controller.HttpContext, statusCode, title, detail, type);
        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }

    public static ObjectResult ToObjectResult(this ProblemDetails problemDetails)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);

        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static string CreateDefaultType(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => $"{ProblemTypeBaseUri}/bad-request",
        StatusCodes.Status401Unauthorized => $"{ProblemTypeBaseUri}/unauthorized",
        StatusCodes.Status403Forbidden => $"{ProblemTypeBaseUri}/forbidden",
        StatusCodes.Status404NotFound => $"{ProblemTypeBaseUri}/not-found",
        StatusCodes.Status409Conflict => $"{ProblemTypeBaseUri}/conflict",
        StatusCodes.Status422UnprocessableEntity => $"{ProblemTypeBaseUri}/validation-error",
        StatusCodes.Status429TooManyRequests => $"{ProblemTypeBaseUri}/rate-limit-exceeded",
        StatusCodes.Status502BadGateway => $"{ProblemTypeBaseUri}/bad-gateway",
        StatusCodes.Status503ServiceUnavailable => $"{ProblemTypeBaseUri}/service-unavailable",
        _ => $"{ProblemTypeBaseUri}/unexpected-error"
    };
}
