using System.Diagnostics.Metrics;

namespace FoodSocial.Post.Api.Observability;

public sealed class PostApiMetrics
{
    internal const string MeterName = "FoodSocial.Application";

    private readonly Counter<long> _mediaUploadsAccepted;
    private readonly Counter<long> _mediaUploadsRejected;
    private readonly Counter<long> _mediaCleanupDue;
    private readonly Counter<long> _mediaCleanupDeleted;
    private readonly Counter<long> _mediaCleanupFailures;
    private readonly Counter<long> _contentReportsCreated;
    private readonly Counter<long> _contentReportsResolved;

    public PostApiMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        var meter = meterFactory.Create(MeterName);
        _mediaUploadsAccepted = meter.CreateCounter<long>("foodsocial_post_media_uploads_accepted_total", "uploads", "Accepted Post media uploads.");
        _mediaUploadsRejected = meter.CreateCounter<long>("foodsocial_post_media_uploads_rejected_total", "uploads", "Rejected Post media upload attempts.");
        _mediaCleanupDue = meter.CreateCounter<long>("foodsocial_post_media_cleanup_due_total", "uploads", "Post media uploads claimed for cleanup.");
        _mediaCleanupDeleted = meter.CreateCounter<long>("foodsocial_post_media_cleanup_deleted_total", "uploads", "Post media uploads deleted by cleanup.");
        _mediaCleanupFailures = meter.CreateCounter<long>("foodsocial_post_media_cleanup_failures_total", "uploads", "Post media upload cleanup failures.");
        _contentReportsCreated = meter.CreateCounter<long>("foodsocial_post_content_reports_created_total", "reports", "Content reports submitted for manual moderation.");
        _contentReportsResolved = meter.CreateCounter<long>("foodsocial_post_content_reports_resolved_total", "reports", "Content reports resolved by manual moderation.");
    }

    public void RecordMediaUploadAccepted(string contentType) =>
        _mediaUploadsAccepted.Add(1, new KeyValuePair<string, object?>("content_type", contentType));

    public void RecordMediaUploadRejected(string reason) =>
        _mediaUploadsRejected.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void RecordMediaCleanupClaimed(int cleanupDueCount)
    {
        if (cleanupDueCount > 0)
        {
            _mediaCleanupDue.Add(cleanupDueCount);
        }
    }

    public void RecordMediaCleanupDeleted() =>
        _mediaCleanupDeleted.Add(1);

    public void RecordMediaCleanupFailure() =>
        _mediaCleanupFailures.Add(1);

    public void RecordContentReportCreated(string targetType) =>
        _contentReportsCreated.Add(1, new KeyValuePair<string, object?>("target_type", targetType));

    public void RecordContentReportResolved(string action) =>
        _contentReportsResolved.Add(1, new KeyValuePair<string, object?>("action", action));
}
