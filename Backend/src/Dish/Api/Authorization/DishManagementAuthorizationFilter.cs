using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodSocial.Dish.Api.Authorization;

internal sealed class DishManagementAuthorizationFilter(IConfiguration configuration) : IAsyncAuthorizationFilter
{
    private const string ManagementKeyHeaderName = "X-FoodSocial-Dish-Management-Key";
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var configuredKey = _configuration["Dish:ManagementApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            context.Result = CreateProblemResult(
                context.HttpContext,
                StatusCodes.Status503ServiceUnavailable,
                "Dish management is not configured",
                "Dish management operations are disabled until a management API key is configured.",
                "https://foodsocial/errors/dish/management-not-configured");
            return Task.CompletedTask;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(ManagementKeyHeaderName, out var providedKey)
            || string.IsNullOrWhiteSpace(providedKey.ToString()))
        {
            context.Result = CreateProblemResult(
                context.HttpContext,
                StatusCodes.Status401Unauthorized,
                "Dish management authorization required",
                "A Dish management credential is required for this operation.",
                "https://foodsocial/errors/dish/management-authorization-required");
            return Task.CompletedTask;
        }

        if (!FixedTimeEquals(providedKey.ToString(), configuredKey))
        {
            context.Result = CreateProblemResult(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "Dish management authorization failed",
                "The supplied Dish management credential is not allowed to perform this operation.",
                "https://foodsocial/errors/dish/management-authorization-failed");
        }

        return Task.CompletedTask;
    }

    private static ObjectResult CreateProblemResult(HttpContext httpContext, int statusCode, string title, string detail, string type) =>
        FoodSocialProblemDetails.Create(httpContext, statusCode, title, detail, type).ToObjectResult();

    private static bool FixedTimeEquals(string providedKey, string configuredKey)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);
        var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);

        return providedBytes.Length == configuredBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, configuredBytes);
    }
}
