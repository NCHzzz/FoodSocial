namespace FoodSocial.Bff;

internal static class BffRequestConventions
{
    private const string BearerPrefix = "Bearer ";

    public static bool HasBearerAuthorizationHeader(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ResolveBearerTokenOrNull(request) is not null;
    }

    public static bool TryNormalizeReturnUrl(string? returnUrl, out string normalizedReturnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            normalizedReturnUrl = "/";
            return true;
        }

        if (!Uri.TryCreate(returnUrl, UriKind.Relative, out _))
        {
            normalizedReturnUrl = string.Empty;
            return false;
        }

        normalizedReturnUrl = returnUrl;
        return true;
    }

    public static string? ResolveBearerTokenOrNull(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TryExtractBearerToken(request.Headers.Authorization.ToString(), out var bearerToken)
            ? bearerToken
            : null;
    }

    private static bool TryExtractBearerToken(string authorizationHeader, out string? bearerToken)
    {
        if (!authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            bearerToken = null;
            return false;
        }

        var normalizedToken = authorizationHeader[BearerPrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            bearerToken = null;
            return false;
        }

        bearerToken = normalizedToken;
        return true;
    }
}
