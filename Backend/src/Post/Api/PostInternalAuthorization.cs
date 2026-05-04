using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodSocial.Post.Api;

internal static class PostInternalAuthorization
{
    internal const string IdentityIdItemName = "FoodSocial.Post.IdentityId";
    private const string InternalSecretHeaderName = "X-FoodSocial-Internal-Secret";
    private const string IdentityIdHeaderName = "X-FoodSocial-Identity-Id";
    private const string RolesHeaderName = "X-FoodSocial-Identity-Roles";

    internal static bool TryAuthorize(HttpContext httpContext, IConfiguration configuration, bool requireModerator, out Guid identityId, out ObjectResult? problem)
    {
        identityId = Guid.Empty;
        problem = null;

        var configuredSecret = configuration["Post:InternalTrust:SharedSecret"];
        if (string.IsNullOrWhiteSpace(configuredSecret))
        {
            problem = CreateProblemResult(
                StatusCodes.Status503ServiceUnavailable,
                "Post internal trust is not configured",
                "Post mutation operations are disabled until internal trust is configured.",
                "https://foodsocial/errors/post/internal-trust-not-configured");
            return false;
        }

        var providedSecret = httpContext.Request.Headers[InternalSecretHeaderName].ToString();
        if (!FixedTimeEquals(providedSecret, configuredSecret))
        {
            problem = CreateProblemResult(
                StatusCodes.Status401Unauthorized,
                "Post internal trust required",
                "A trusted BFF credential is required for this operation.",
                "https://foodsocial/errors/post/internal-trust-required");
            return false;
        }

        if (!Guid.TryParse(httpContext.Request.Headers[IdentityIdHeaderName].ToString(), out identityId) || identityId == Guid.Empty)
        {
            problem = CreateProblemResult(
                StatusCodes.Status401Unauthorized,
                "Post caller identity required",
                "A trusted caller identity is required for this operation.",
                "https://foodsocial/errors/post/caller-identity-required");
            return false;
        }

        if (requireModerator && !HasModeratorRole(httpContext.Request.Headers[RolesHeaderName].ToString()))
        {
            problem = CreateProblemResult(
                StatusCodes.Status403Forbidden,
                "Post moderation authorization required",
                "A moderator or admin role is required for this operation.",
                "https://foodsocial/errors/post/moderation-authorization-required");
            return false;
        }

        httpContext.Items[IdentityIdItemName] = identityId;
        return true;
    }

    internal static Guid GetIdentityId(HttpContext httpContext) =>
        httpContext.Items.TryGetValue(IdentityIdItemName, out var value) && value is Guid identityId && identityId != Guid.Empty
            ? identityId
            : throw new InvalidOperationException("Post internal authorization did not resolve a caller identity.");

    private static bool HasModeratorRole(string rolesHeader) =>
        rolesHeader
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(role => string.Equals(role, "moderator", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase));

    private static ObjectResult CreateProblemResult(int statusCode, string title, string detail, string type) =>
        new(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type
        })
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };

    private static bool FixedTimeEquals(string candidate, string expected)
    {
        if (string.IsNullOrEmpty(candidate) || string.IsNullOrEmpty(expected))
        {
            return false;
        }

        var candidateBytes = Encoding.UTF8.GetBytes(candidate);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return candidateBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(candidateBytes, expectedBytes);
    }
}

internal sealed class PostInternalAuthorizationFilter(IConfiguration configuration) : IAsyncAuthorizationFilter
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!PostInternalAuthorization.TryAuthorize(context.HttpContext, _configuration, requireModerator: false, out _, out var problem))
        {
            context.Result = problem;
        }

        return Task.CompletedTask;
    }
}

internal sealed class PostModeratorAuthorizationFilter(IConfiguration configuration) : IAsyncAuthorizationFilter
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!PostInternalAuthorization.TryAuthorize(context.HttpContext, _configuration, requireModerator: true, out _, out var problem))
        {
            context.Result = problem;
        }

        return Task.CompletedTask;
    }
}
