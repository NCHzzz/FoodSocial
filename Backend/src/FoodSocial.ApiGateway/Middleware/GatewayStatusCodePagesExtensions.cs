using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.ApiGateway;

internal static class GatewayStatusCodePagesExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IApplicationBuilder UseGatewayStatusCodePages(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseStatusCodePages(async context =>
        {
            var httpContext = context.HttpContext;
            if (httpContext.Response.HasStarted
                || !string.IsNullOrWhiteSpace(httpContext.Response.ContentType)
                || httpContext.Response.StatusCode < 400)
            {
                return;
            }

            var problem = CreateProblemDetails(httpContext);
            httpContext.Response.ContentType = "application/problem+json";
            await JsonSerializer.SerializeAsync(httpContext.Response.Body, problem, JsonOptions, httpContext.RequestAborted);
        });
    }

    private static ProblemDetails CreateProblemDetails(HttpContext context)
    {
        var status = context.Response.StatusCode;
        var title = status switch
        {
            StatusCodes.Status404NotFound => "Gateway route not found",
            StatusCodes.Status502BadGateway => "Downstream service unavailable",
            StatusCodes.Status503ServiceUnavailable => "Downstream service unavailable",
            StatusCodes.Status504GatewayTimeout => "Downstream service timed out",
            _ => "Gateway request failed"
        };

        var type = status switch
        {
            StatusCodes.Status404NotFound => "https://foodsocial/errors/gateway/route-not-found",
            StatusCodes.Status502BadGateway => "https://foodsocial/errors/gateway/downstream-unavailable",
            StatusCodes.Status503ServiceUnavailable => "https://foodsocial/errors/gateway/downstream-unavailable",
            StatusCodes.Status504GatewayTimeout => "https://foodsocial/errors/gateway/downstream-timeout",
            _ => "https://foodsocial/errors/gateway/request-failed"
        };

        return new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = "The API Gateway could not complete the request.",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        };
    }
}
