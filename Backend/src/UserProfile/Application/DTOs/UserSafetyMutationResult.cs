using FoodSocial.UserProfile.Domain;

namespace FoodSocial.UserProfile.Application.DTOs;

/// <summary>
/// Result of a safety relationship mutation (block, mute, unblock, unmute).
/// </summary>
public sealed record UserSafetyMutationResult(
    UserProfileOperationStatus Status,
    Guid TargetIdentityId,
    RelationshipType Type,
    bool IsActive,
    string? Error = null);
