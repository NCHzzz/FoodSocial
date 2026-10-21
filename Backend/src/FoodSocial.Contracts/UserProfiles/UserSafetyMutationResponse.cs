namespace FoodSocial.Contracts.UserProfiles;

/// <summary>
/// Response contract for block/mute/block-unmute mutation results.
/// </summary>
/// <param name="TargetIdentityId">The identity that was targeted by the safety operation.</param>
/// <param name="Type">"Block" or "Mute".</param>
/// <param name="IsActive"><c>true</c> when the relationship is active (created), <c>false</c> when removed.</param>
public sealed record UserSafetyMutationResponse(
    Guid TargetIdentityId,
    string Type,
    bool IsActive);
