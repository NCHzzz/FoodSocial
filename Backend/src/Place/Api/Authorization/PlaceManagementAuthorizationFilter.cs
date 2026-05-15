using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodSocial.Place.Api.Authorization;

internal sealed class PlaceManagementAuthorizationFilter(IConfiguration configuration) : IAsyncAuthorizationFilter
{
    private const string ManagementKeyHeaderName = "X-FoodSocial-Place-Management-Key";
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var configuredKey = _configuration["Place:ManagementApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            context.Result = CreateProblemResult(
                context.HttpContext,
                StatusCodes.Status503ServiceUnavailable,
                "Place management is not configured",
                "Place management operations are disabled until a management API key is configured.",
                "https://foodsocial/errors/place/management-not-configured");
            return Task.CompletedTask;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(ManagementKeyHeaderName, out var providedKey)
            || string.IsNullOrWhiteSpace(providedKey.ToString()))
        {
            context.Result = CreateProblemResult(
                context.HttpContext,
                StatusCodes.Status401Unauthorized,
                "Place management authorization required",
                "A Place management credential is required for this operation.",
                "https://foodsocial/errors/place/management-authorization-required");
            return Task.CompletedTask;
        }

        if (!FixedTimeEquals(providedKey.ToString(), configuredKey))
        {
            context.Result = CreateProblemResult(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "Place management authorization failed",
                "The supplied Place management credential is not allowed to perform this operation.",
                "https://foodsocial/errors/place/management-authorization-failed");
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
