using System.Security.Cryptography;
using System.Text;
using FoodSocial.Contracts.Identity;
using Microsoft.AspNetCore.Authorization;

namespace FoodSocial.Identity.Api.Authorization;

/// <summary>
/// Evaluates explicit service-to-service trust credentials.
/// </summary>
public sealed class TrustedInternalServiceAuthorizationHandler(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<TrustedInternalServiceRequirement>
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TrustedInternalServiceRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return Task.CompletedTask;
        }

        var configuredServiceId = _configuration["Identity:InternalTrust:ServiceId"];
        var configuredSecret = _configuration["Identity:InternalTrust:SharedSecret"];

        if (string.IsNullOrWhiteSpace(configuredServiceId)
            || string.IsNullOrWhiteSpace(configuredSecret))
        {
            return Task.CompletedTask;
        }

        var callerServiceId = httpContext.Request.Headers["X-Internal-Service-Id"].ToString();
        var callerSecret = httpContext.Request.Headers["X-Internal-Service-Secret"].ToString();

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var authorizedParty = IdentityClaimReader.GetAuthorizedParty(context.User);

        if (IsExactSecretMatch(callerServiceId, configuredServiceId)
            && IsExactSecretMatch(callerSecret, configuredSecret)
            && string.Equals(authorizedParty, configuredServiceId, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool IsExactSecretMatch(string candidate, string expected)
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
