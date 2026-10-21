namespace FoodSocial.Post.Domain;

/// <summary>
/// Represents the moderation outcome for a content report.
/// </summary>
public enum ReportStatus
{
    Submitted = 0,
    ResolvedNoAction = 1,
    ResolvedContentAction = 2
}
