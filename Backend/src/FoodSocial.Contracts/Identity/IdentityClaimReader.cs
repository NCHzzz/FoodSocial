using System.Security.Claims;
using System.Text.Json;

namespace FoodSocial.Contracts.Identity;

public static class IdentityClaimReader
{
    public static string? GetSubject(ClaimsPrincipal user) =>
        FindFirstValue(user, IdentityClaims.Subject)
        ?? FindFirstValue(user, "sub")
        ?? FindFirstValue(user, ClaimTypes.NameIdentifier)
        ?? FindFirstValue(user, "nameidentifier");

    public static string? GetPreferredUsername(ClaimsPrincipal user) =>
        FindFirstValue(user, IdentityClaims.PreferredUsername)
        ?? FindFirstValue(user, "name")
        ?? user.Identity?.Name;

    public static string? GetEmail(ClaimsPrincipal user) =>
        FindFirstValue(user, ClaimTypes.Email)
        ?? FindFirstValue(user, IdentityClaims.Email)
        ?? FindFirstValue(user, "email");

    public static string? GetAuthorizedParty(ClaimsPrincipal user) =>
        FindFirstValue(user, "azp")
        ?? FindFirstValue(user, "client_id");

    public static IReadOnlyList<string> GetRoles(ClaimsPrincipal user)
    {
        var roles = user.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var claim in user.FindAll(IdentityClaims.RealmAccess))
        {
            AddJsonRoles(claim.Value, roles);
        }

        foreach (var claim in user.FindAll(IdentityClaims.ResourceAccess))
        {
            AddResourceRoles(claim.Value, roles);
        }

        return roles.OrderBy(role => role, StringComparer.Ordinal).ToArray();
    }

    private static void AddJsonRoles(string claimValue, ISet<string> roles)
    {
        using var document = JsonDocument.Parse(claimValue);

        if (!document.RootElement.TryGetProperty(IdentityClaims.Roles, out var rolesElement)
            || rolesElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var roleElement in rolesElement.EnumerateArray())
        {
            var role = roleElement.GetString();

            if (!string.IsNullOrWhiteSpace(role))
            {
                roles.Add(role);
            }
        }
    }

    private static void AddResourceRoles(string claimValue, ISet<string> roles)
    {
        using var document = JsonDocument.Parse(claimValue);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!property.Value.TryGetProperty(IdentityClaims.Roles, out var rolesElement)
                || rolesElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var roleElement in rolesElement.EnumerateArray())
            {
                var role = roleElement.GetString();

                if (!string.IsNullOrWhiteSpace(role))
                {
                    roles.Add(role);
                }
            }
        }
    }

    private static string? FindFirstValue(ClaimsPrincipal user, string claimType)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        return user.Claims.FirstOrDefault(claim => string.Equals(claim.Type, claimType, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}
