namespace FoodSocial.Contracts.Common;

/// <summary>
/// Canonical tenant defaults shared across FoodSocial services.
/// </summary>
/// <remarks>
/// The launch posture keeps a single default tenant and treats tenant identity as an
/// architectural isolation concern rather than a primary product behavior. The literal
/// value <c>00000000-0000-0000-0000-000000000001</c> is the agreed default tenant id
/// used by every service-owned persistence boundary.
/// </remarks>
public static class TenantDefaults
{
    /// <summary>
    /// The default tenant identifier applied when a service-owned persistence boundary
    /// has not yet been wired to a real tenant resolver.
    /// </summary>
    public static readonly Guid Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
}
