using System.Security.Claims;
using System.Text.Encodings.Web;
using FoodSocial.Contracts.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodSocial.Identity.Api.Tests;

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string SchemeName = "TestBearer";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>();

        AddClaim("X-Test-Sub", IdentityClaims.Subject);
        AddClaim("X-Test-NameIdentifier", ClaimTypes.NameIdentifier);
        AddClaim("X-Test-PreferredUsername", IdentityClaims.PreferredUsername);
        AddClaim("X-Test-Email", IdentityClaims.Email);
        AddClaim("X-Test-Azp", "azp");
        AddClaim("X-Test-Client-Id", "client_id");
        AddClaim("X-Test-Realm-Access", IdentityClaims.RealmAccess);
        AddClaim("X-Test-Resource-Access", IdentityClaims.ResourceAccess);

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));

        void AddClaim(string headerName, string claimType)
        {
            if (Request.Headers.TryGetValue(headerName, out var values)
                && !string.IsNullOrWhiteSpace(values.ToString()))
            {
                claims.Add(new Claim(claimType, values.ToString()));
            }
        }
    }
}
