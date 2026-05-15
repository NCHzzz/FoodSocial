using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FoodSocial.ApiGateway;

internal static class GatewayRequestBodyLimitMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IApplicationBuilder UseGatewayRequestBodyLimit(this IApplicationBuilder app, GatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        return app.Use(async (context, next) =>
        {
            if (context.Request.ContentLength is > 0
                && context.Request.ContentLength > options.MaxRequestBodySizeBytes)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                context.Response.ContentType = "application/problem+json";
                var problem = new ProblemDetails
                {
                    Type = "https://foodsocial/errors/gateway/request-body-too-large",
                    Title = "Request body too large",
                    Detail = "The request body exceeds the API Gateway size limit.",
                    Status = StatusCodes.Status413PayloadTooLarge,
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = context.TraceIdentifier,
                        ["maxRequestBodySizeBytes"] = options.MaxRequestBodySizeBytes
                    }
                };

                await JsonSerializer.SerializeAsync(context.Response.Body, problem, JsonOptions, context.RequestAborted);
                return;
            }

            await next(context);
        });
    }
}
