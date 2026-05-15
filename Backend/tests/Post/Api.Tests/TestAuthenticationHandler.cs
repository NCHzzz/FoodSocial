using System.Security.Claims;
using System.Text.Encodings.Web;
using FoodSocial.Contracts.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodSocial.Post.Api.Tests;

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string SchemeName = "TestBearer";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizedParty = Request.Headers["X-Test-Azp"].ToString();
        if (string.IsNullOrWhiteSpace(authorizedParty))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim("azp", authorizedParty),
            new Claim("client_id", authorizedParty),
            new Claim(IdentityClaims.Subject, authorizedParty)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
