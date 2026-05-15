using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application.DTOs;
using FoodSocial.Post.Application.UseCases;
using FoodSocial.Post.Domain;

namespace FoodSocial.Post.Application.UseCases.Posts;

public sealed class PostService(
    PostPublishingUseCase publishingUseCase,
    PostQueryUseCase queryUseCase,
    PostEngagementUseCase engagementUseCase,
    PostCommentUseCase commentUseCase,
    PostRepostUseCase repostUseCase,
    PostReportUseCase reportUseCase,
    PostNotificationUseCase notificationUseCase,
    PostMediaUploadUseCase mediaUploadUseCase)
{
    private readonly PostPublishingUseCase _publishingUseCase = publishingUseCase ?? throw new ArgumentNullException(nameof(publishingUseCase));
    private readonly PostQueryUseCase _queryUseCase = queryUseCase ?? throw new ArgumentNullException(nameof(queryUseCase));
    private readonly PostEngagementUseCase _engagementUseCase = engagementUseCase ?? throw new ArgumentNullException(nameof(engagementUseCase));
    private readonly PostCommentUseCase _commentUseCase = commentUseCase ?? throw new ArgumentNullException(nameof(commentUseCase));
    private readonly PostRepostUseCase _repostUseCase = repostUseCase ?? throw new ArgumentNullException(nameof(repostUseCase));
    private readonly PostReportUseCase _reportUseCase = reportUseCase ?? throw new ArgumentNullException(nameof(reportUseCase));
    private readonly PostNotificationUseCase _notificationUseCase = notificationUseCase ?? throw new ArgumentNullException(nameof(notificationUseCase));
    private readonly PostMediaUploadUseCase _mediaUploadUseCase = mediaUploadUseCase ?? throw new ArgumentNullException(nameof(mediaUploadUseCase));

    public async Task<PostMutationResult> CreatePostAsync(CreatePostRequest request, CancellationToken cancellationToken) =>
        await _publishingUseCase.CreatePostAsync(request, cancellationToken);

    public async Task<PostDetailResponse?> GetPostAsync(Guid postId, CancellationToken cancellationToken) =>
        await _queryUseCase.GetPostAsync(postId, cancellationToken);

    public async Task<PostMediaUploadRecord?> GetDeliverableMediaUploadAsync(Guid uploadId, Guid? callerIdentityId, CancellationToken cancellationToken)
    {
        if (uploadId == Guid.Empty)
        {
            return null;
        }

        return await _mediaUploadUseCase.GetDeliverableMediaUploadAsync(uploadId, callerIdentityId, cancellationToken);
    }

    public async Task<PostSearchResponse> SearchPostsAsync(PostSearchCriteria criteria, CancellationToken cancellationToken) =>
        await _queryUseCase.SearchPostsAsync(criteria, cancellationToken);

    public async Task<DiscoverFeedResponse> GetDiscoverFeedAsync(DiscoverFeedCriteria criteria, CancellationToken cancellationToken) =>
        await _queryUseCase.GetDiscoverFeedAsync(criteria, cancellationToken);

    public async Task<PostLikeResponse?> AddLikeAsync(Guid postId, Guid actorId, CancellationToken cancellationToken) =>
        await _engagementUseCase.AddLikeAsync(postId, actorId, cancellationToken);

    public async Task<PostLikeResponse?> RemoveLikeAsync(Guid postId, Guid actorId, CancellationToken cancellationToken) =>
        await _engagementUseCase.RemoveLikeAsync(postId, actorId, cancellationToken);

    public async Task<CommentMutationResult> AddCommentAsync(Guid postId, CreatePostCommentRequest request, CancellationToken cancellationToken) =>
        await _commentUseCase.AddCommentAsync(postId, request, cancellationToken);

    public async Task<RepostMutationResult> AddRepostAsync(Guid postId, CreatePostRepostRequest request, CancellationToken cancellationToken) =>
        await _repostUseCase.AddRepostAsync(postId, request, cancellationToken);

    public async Task<CommentMutationResult> DeleteCommentAsync(Guid commentId, Guid actorId, CancellationToken cancellationToken) =>
        await _commentUseCase.DeleteCommentAsync(commentId, actorId, cancellationToken);

    public async Task<ReportMutationResult> ReportPostAsync(Guid postId, CreateContentReportRequest request, CancellationToken cancellationToken) =>
        await _reportUseCase.ReportPostAsync(postId, request, cancellationToken);

    public async Task<ReportMutationResult> ReportCommentAsync(Guid commentId, CreateContentReportRequest request, CancellationToken cancellationToken) =>
        await _reportUseCase.ReportCommentAsync(commentId, request, cancellationToken);

    public async Task<AdminReportsResponse> GetReportsAsync(int? limit, CancellationToken cancellationToken) =>
        await _reportUseCase.GetReportsAsync(limit, cancellationToken);

    public async Task<ReportMutationResult> ResolveReportAsync(Guid reportId, ResolveContentReportRequest request, CancellationToken cancellationToken) =>
        await _reportUseCase.ResolveReportAsync(reportId, request, cancellationToken);

    public async Task<NotificationListResponse> GetNotificationsAsync(Guid recipientId, int? limit, CancellationToken cancellationToken) =>
        await _notificationUseCase.GetNotificationsAsync(recipientId, limit, cancellationToken);

    public async Task<NotificationMutationResult> MarkNotificationReadAsync(Guid notificationId, Guid recipientId, CancellationToken cancellationToken) =>
        await _notificationUseCase.MarkNotificationReadAsync(notificationId, recipientId, cancellationToken);

    public async Task RecordMediaUploadAsync(RegisterMediaUploadCommand command, CancellationToken cancellationToken) =>
        await _mediaUploadUseCase.RecordMediaUploadAsync(command, cancellationToken);

    public async Task<IReadOnlyList<PostMediaUploadRecord>> ClaimMediaUploadsDueForCleanupAsync(DateTimeOffset utcNow, int? limit, CancellationToken cancellationToken) =>
        await _mediaUploadUseCase.ClaimMediaUploadsDueForCleanupAsync(utcNow, limit, cancellationToken);

    public async Task MarkMediaUploadDeletedAsync(Guid uploadId, CancellationToken cancellationToken) =>
        await _mediaUploadUseCase.MarkMediaUploadDeletedAsync(uploadId, cancellationToken);
}
