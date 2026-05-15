using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Bff;

internal sealed class BffCsrfEndpointFilter : IEndpointFilter
{
    internal static readonly BffCsrfEndpointFilter Instance = new();

    private BffCsrfEndpointFilter()
    {
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var httpContext = context.HttpContext;
        if (ShouldValidateRequest(httpContext.Request))
        {
            var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.Problem(new ProblemDetails
                {
                    Type = "https://foodsocial/errors/bff/csrf-validation-failed",
                    Title = "CSRF token validation failed",
                    Detail = "A valid CSRF token is required for this cookie-authenticated request.",
                    Status = StatusCodes.Status400BadRequest,
                    Extensions =
                    {
                        ["traceId"] = httpContext.TraceIdentifier
                    }
                });
            }
        }

        return await next(context);
    }

    private static bool ShouldValidateRequest(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method)
            || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method)
            || HttpMethods.IsTrace(request.Method))
        {
            return false;
        }

        return !BffRequestConventions.HasBearerAuthorizationHeader(request)
            && request.Cookies.ContainsKey("__Host-FoodSocial.Bff");
    }
}
