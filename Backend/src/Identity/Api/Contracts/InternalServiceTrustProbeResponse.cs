namespace FoodSocial.Identity.Api.Contracts;

/// <summary>
/// Represents the result of an internal-service trust probe.
/// </summary>
/// <param name="Trusted">Whether explicit trust credentials were accepted.</param>
public sealed record InternalServiceTrustProbeResponse(bool Trusted);
