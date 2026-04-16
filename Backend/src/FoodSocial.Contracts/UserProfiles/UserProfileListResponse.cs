namespace FoodSocial.Contracts.UserProfiles;

/// <summary>
/// Represents a bounded follow-graph list response.
/// </summary>
/// <param name="Items">The returned profile summaries.</param>
/// <param name="TotalCount">The total number of relationships for the list.</param>
public sealed record UserProfileListResponse(
    IReadOnlyList<UserProfileSummaryDto> Items,
    long TotalCount);
