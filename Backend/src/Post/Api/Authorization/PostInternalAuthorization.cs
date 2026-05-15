using FoodSocial.Contracts.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodSocial.Post.Api.Authorization;

internal static class PostInternalAuthorization
{
    internal const string IdentityIdItemName = "FoodSocial.Post.IdentityId";
    private const string IdentityIdHeaderName = "X-FoodSocial-Identity-Id";
    private const string RolesHeaderName = "X-FoodSocial-Identity-Roles";

    internal static bool TryAuthorize(HttpContext httpContext, IConfiguration configuration, ILogger logger, bool requireModerator, out Guid identityId, out ObjectResult? problem)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        identityId = Guid.Empty;
        problem = null;

        var configuredServiceClientId = configuration["Post:TrustedServiceClients:BffClientId"];
        if (string.IsNullOrWhiteSpace(configuredServiceClientId))
        {
            logger.LogError(
                "Post trusted service client configuration is missing. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
            problem = CreateProblemResult(
                httpContext,
                StatusCodes.Status503ServiceUnavailable,
                "Post trusted service client is not configured",
                "Post mutation operations are disabled until trusted service authentication is configured.",
                "https://foodsocial/errors/post/trusted-service-client-not-configured");
            return false;
        }

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning(
                "Post trusted service request was unauthenticated from {RemoteIpAddress}. TraceId: {TraceId}",
                httpContext.Connection.RemoteIpAddress,
                httpContext.TraceIdentifier);
            problem = CreateProblemResult(
                httpContext,
                StatusCodes.Status401Unauthorized,
                "Post trusted service token required",
                "A trusted BFF service token is required for this operation.",
                "https://foodsocial/errors/post/trusted-service-token-required");
            return false;
        }

        var authorizedParty = IdentityClaimReader.GetAuthorizedParty(httpContext.User);
        if (!string.Equals(authorizedParty, configuredServiceClientId, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Post trusted service client validation failed for authorized party {AuthorizedParty}. TraceId: {TraceId}",
                authorizedParty,
                httpContext.TraceIdentifier);
            problem = CreateProblemResult(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Post trusted service client unauthorized",
                "The service token is not authorized for Post mutations.",
                "https://foodsocial/errors/post/trusted-service-client-unauthorized");
            return false;
        }

        if (!Guid.TryParse(httpContext.Request.Headers[IdentityIdHeaderName].ToString(), out identityId) || identityId == Guid.Empty)
        {
            logger.LogWarning(
                "Post trusted service request omitted a valid caller identity. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
            problem = CreateProblemResult(
                httpContext,
                StatusCodes.Status401Unauthorized,
                "Post caller identity required",
                "A trusted caller identity is required for this operation.",
                "https://foodsocial/errors/post/caller-identity-required");
            return false;
        }

        var rolesHeader = httpContext.Request.Headers[RolesHeaderName].ToString();
        if (requireModerator && !HasModeratorRole(rolesHeader))
        {
            logger.LogWarning(
                "Post moderation trust request for identity {IdentityId} lacked moderator role. TraceId: {TraceId}",
                identityId,
                httpContext.TraceIdentifier);
            problem = CreateProblemResult(
                httpContext,
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

    private static ObjectResult CreateProblemResult(HttpContext httpContext, int statusCode, string title, string detail, string type) =>
        FoodSocialProblemDetails.Create(httpContext, statusCode, title, detail, type).ToObjectResult();

}

internal sealed class PostInternalAuthorizationFilter(IConfiguration configuration, ILogger<PostInternalAuthorizationFilter> logger) : IAsyncAuthorizationFilter
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ILogger<PostInternalAuthorizationFilter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!PostInternalAuthorization.TryAuthorize(context.HttpContext, _configuration, _logger, requireModerator: false, out _, out var problem))
        {
            context.Result = problem;
        }

        return Task.CompletedTask;
    }
}

internal sealed class PostModeratorAuthorizationFilter(IConfiguration configuration, ILogger<PostModeratorAuthorizationFilter> logger) : IAsyncAuthorizationFilter
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ILogger<PostModeratorAuthorizationFilter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!PostInternalAuthorization.TryAuthorize(context.HttpContext, _configuration, _logger, requireModerator: true, out _, out var problem))
        {
            context.Result = problem;
        }

        return Task.CompletedTask;
    }
}
