namespace FoodSocial.Post.Domain;

/// <summary>
/// Represents a user-submitted content report awaiting or completing moderation review.
/// </summary>
public sealed class ContentReportRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public Guid ReporterId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Submitted;
    public Guid? ModeratorId { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAtUtc { get; set; }
}
