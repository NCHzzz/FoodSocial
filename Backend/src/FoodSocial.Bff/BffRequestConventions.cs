namespace FoodSocial.Bff;

internal static class BffRequestConventions
{
    private const string BearerPrefix = "Bearer ";

    public static bool HasBearerAuthorizationHeader(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Headers.Authorization.ToString().StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase);
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

    public static string? TryResolveBearerToken(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authorizationHeader = request.Headers.Authorization.ToString();
        return authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader[BearerPrefix.Length..].Trim()
            : null;
    }
}
