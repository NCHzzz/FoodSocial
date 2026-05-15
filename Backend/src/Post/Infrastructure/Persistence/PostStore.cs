using FoodSocial.Contracts.Posts;
using FoodSocial.Messaging;
using FoodSocial.Post.Application;
using FoodSocial.Post.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodSocial.Post.Infrastructure.Persistence;

public sealed partial class PostStore(PostDbContext dbContext) : IPostStore
{
    private readonly PostDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<PostRecord> CreatePostAsync(PostRecord post, CancellationToken cancellationToken)
    {
        _dbContext.Posts.Add(post);
        _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(post, "Created"));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return post;
    }

    public async Task<PostRecord> CreatePostWithMediaUploadsAsync(PostRecord post, IReadOnlyList<PostMediaUploadAttachmentRequest> mediaUploadsToAttach, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var mediaUrlsToAttach = mediaUploadsToAttach.Select(upload => upload.Url).Distinct(StringComparer.Ordinal).ToArray();
        var uploads = await CurrentTenantUploads()
            .AsNoTracking()
            .Where(upload => upload.OwnerId == post.AuthorId
                && upload.Status == PostMediaUploadStatus.StoredPendingAttachment
                && mediaUrlsToAttach.Contains(upload.Url))
            .ToListAsync(cancellationToken);

        var attachableUrls = BuildAttachableUploadUrlSet(mediaUploadsToAttach, uploads);
        if (mediaUrlsToAttach.Any(url => !attachableUrls.Contains(url)))
        {
            throw new PostMediaUploadAttachmentException("One or more media uploads are not attachable.");
        }

        _dbContext.Posts.Add(post);
        _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(post, "Created"));
        await _dbContext.SaveChangesAsync(cancellationToken);
        var attachedAtUtc = DateTimeOffset.UtcNow;
        var attachedCount = await CurrentTenantUploads()
            .Where(upload => upload.OwnerId == post.AuthorId
                && upload.Status == PostMediaUploadStatus.StoredPendingAttachment
                && mediaUrlsToAttach.Contains(upload.Url))
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(upload => upload.AttachedPostId, post.Id)
                    .SetProperty(upload => upload.Status, PostMediaUploadStatus.Attached)
                    .SetProperty(upload => upload.AttachedAtUtc, attachedAtUtc),
                cancellationToken);

        if (attachedCount != mediaUrlsToAttach.Length)
        {
            throw new PostMediaUploadAttachmentException("One or more media uploads could not be atomically attached.");
        }

        await transaction.CommitAsync(cancellationToken);
        return post;
    }

    public async Task<PostRecord?> GetPostAsync(Guid postId, CancellationToken cancellationToken) =>
        await IncludePostGraph(CurrentTenantPosts())
            .AsSplitQuery()
            .FirstOrDefaultAsync(post => post.Id == postId && post.Status == PostStatus.Published, cancellationToken);

    public async Task<PostMediaUploadRecord?> GetDeliverableMediaUploadAsync(Guid uploadId, Guid? callerIdentityId, CancellationToken cancellationToken)
    {
        var upload = await CurrentTenantUploads()
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == uploadId && !candidate.ModerationHold, cancellationToken);
        if (upload is null)
        {
            return null;
        }

        if (upload.Status == PostMediaUploadStatus.StoredPendingAttachment)
        {
            return callerIdentityId == upload.OwnerId ? upload : null;
        }

        if (upload.Status != PostMediaUploadStatus.Attached || upload.AttachedPostId is null)
        {
            return null;
        }

        var attachedPostIsPublished = await CurrentTenantPosts()
            .AsNoTracking()
            .AnyAsync(post => post.Id == upload.AttachedPostId && post.Status == PostStatus.Published, cancellationToken);
        return attachedPostIsPublished ? upload : null;
    }

    public async Task<IReadOnlyList<PostRecord>> SearchPostsAsync(PostSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = CurrentTenantPosts();
        if (!criteria.IncludeHidden) query = query.Where(post => post.Status == PostStatus.Published);
        if (criteria.AuthorIds.Count > 0) query = query.Where(post => criteria.AuthorIds.Contains(post.AuthorId));
        if (criteria.DishId is { } dishId) query = query.Where(post => post.DishId == dishId);
        if (criteria.PlaceId is { } placeId) query = query.Where(post => post.PlaceId == placeId);
        if (!string.IsNullOrWhiteSpace(criteria.Tag)) query = ApplyTagFilter(query, criteria.Tag);
        if (!string.IsNullOrWhiteSpace(criteria.Query)) query = ApplyTextSearch(query, criteria.Query);
        var postIds = IsSqliteProvider()
            ? (await query.AsNoTracking().ToListAsync(cancellationToken))
                .OrderByDescending(post => post.CreatedAtUtc)
                .ThenBy(post => post.Id)
                .Take(criteria.Limit)
                .Select(post => post.Id)
                .ToList()
            : await query
                .AsNoTracking()
                .OrderByDescending(post => post.CreatedAtUtc)
                .ThenBy(post => post.Id)
                .Select(post => post.Id)
                .Take(criteria.Limit)
                .ToListAsync(cancellationToken);
        return await LoadPostsByIdsAsync(postIds, cancellationToken);
    }

    public async Task<IReadOnlyList<PostRecord>> GetDiscoverFeedAsync(DiscoverFeedCriteria criteria, CancellationToken cancellationToken)
    {
        var publishedPosts = CurrentTenantPosts()
            .AsNoTracking()
            .Where(post => post.Status == PostStatus.Published);

        var postIds = IsSqliteProvider()
            ? (await publishedPosts
                .Include(post => post.Likes)
                .Include(post => post.Comments)
                .AsSplitQuery()
                .ToListAsync(cancellationToken))
                .OrderByDescending(post => post.CreatedAtUtc)
                .ThenByDescending(post => post.Likes.Count + post.Comments.Count(comment => comment.Status == CommentStatus.Visible))
                .ThenBy(post => post.Id)
                .Take(criteria.Limit)
                .Select(post => post.Id)
                .ToList()
            : await publishedPosts
                .OrderByDescending(post => post.CreatedAtUtc)
                .ThenByDescending(post => post.Likes.Count + post.Comments.Count(comment => comment.Status == CommentStatus.Visible))
                .ThenBy(post => post.Id)
                .Select(post => post.Id)
                .Take(criteria.Limit)
                .ToListAsync(cancellationToken);

        return await LoadPostsByIdsAsync(postIds, cancellationToken);
    }

    public async Task<int?> AddLikeAsync(Guid postId, Guid actorId, CancellationToken cancellationToken)
    {
        var post = await CurrentTenantPosts().FirstOrDefaultAsync(candidate => candidate.Id == postId && candidate.Status == PostStatus.Published, cancellationToken);
        if (post is null)
        {
            return null;
        }

        var alreadyLiked = await CurrentTenantPostLikes().AnyAsync(like => like.PostId == postId && like.ActorId == actorId, cancellationToken);
        if (!alreadyLiked)
        {
            _dbContext.PostLikes.Add(new PostLikeRecord { PostId = postId, ActorId = actorId });
            AddNotification(post.AuthorId, actorId, "PostLiked", "Post", postId, postId, "liked your food post");
            _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(post, "Liked"));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        return await CurrentTenantPostLikes().CountAsync(like => like.PostId == postId, cancellationToken);
    }

    public async Task<int?> RemoveLikeAsync(Guid postId, Guid actorId, CancellationToken cancellationToken)
    {
        var post = await CurrentTenantPosts().FirstOrDefaultAsync(candidate => candidate.Id == postId && candidate.Status == PostStatus.Published, cancellationToken);
        if (post is null)
        {
            return null;
        }

        var like = await CurrentTenantPostLikes().FirstOrDefaultAsync(item => item.PostId == postId && item.ActorId == actorId, cancellationToken);
        if (like is not null)
        {
            _dbContext.PostLikes.Remove(like);
            _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(post, "Unliked"));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await CurrentTenantPostLikes().CountAsync(item => item.PostId == postId, cancellationToken);
    }

    public async Task<PostCommentRecord?> AddCommentAsync(Guid postId, Guid authorId, string body, CancellationToken cancellationToken)
    {
        var post = await CurrentTenantPosts().FirstOrDefaultAsync(candidate => candidate.Id == postId && candidate.Status == PostStatus.Published, cancellationToken);
        if (post is null)
        {
            return null;
        }

        var comment = new PostCommentRecord
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            AuthorId = authorId,
            Body = body.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        _dbContext.PostComments.Add(comment);
        AddNotification(post.AuthorId, authorId, "PostCommented", "Comment", comment.Id, postId, "commented on your food post");
        _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(post, "Commented"));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return comment;
    }

    public async Task<PostCommentRecord?> GetCommentAsync(Guid commentId, CancellationToken cancellationToken) => await CurrentTenantComments().FirstOrDefaultAsync(comment => comment.Id == commentId, cancellationToken);

    public async Task<PostCommentRecord?> SoftDeleteCommentAsync(Guid commentId, Guid actorId, CancellationToken cancellationToken)
    {
        var comment = await GetCommentAsync(commentId, cancellationToken);
        if (comment is null || comment.AuthorId != actorId)
        {
            return null;
        }

        comment.Status = CommentStatus.Deleted;
        comment.UpdatedAtUtc = DateTimeOffset.UtcNow;
        var post = await CurrentTenantPosts().FirstOrDefaultAsync(candidate => candidate.Id == comment.PostId, cancellationToken);
        if (post is not null)
        {
            _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(post, "CommentDeleted"));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return comment;
    }

    public async Task<PostRepostRecord?> AddRepostAsync(Guid postId, Guid actorId, string? caption, CancellationToken cancellationToken)
    {
        var post = await CurrentTenantPosts().FirstOrDefaultAsync(candidate => candidate.Id == postId && candidate.Status == PostStatus.Published, cancellationToken);
        if (post is null) return null;

        var normalizedCaption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim();
        var existing = await CurrentTenantPostReposts().FirstOrDefaultAsync(repost => repost.PostId == postId && repost.ActorId == actorId, cancellationToken);
        if (existing is not null)
        {
            existing.Caption = normalizedCaption;
            _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(post, "RepostUpdated"));
            await _dbContext.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var repost = new PostRepostRecord
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            ActorId = actorId,
            Caption = normalizedCaption,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _dbContext.PostReposts.Add(repost);
        AddNotification(post.AuthorId, actorId, "PostReposted", "Repost", repost.Id, postId, "reposted your food post");
        _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(post, "Reposted"));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return repost;
    }

    public Task<int> CountRepostsAsync(Guid postId, CancellationToken cancellationToken) =>
        CurrentTenantPostReposts().CountAsync(repost => repost.PostId == postId, cancellationToken);

    public async Task<ContentReportRecord?> CreateReportAsync(ReportTargetType targetType, Guid targetId, Guid reporterId, string reason, string? description, CancellationToken cancellationToken)
    {
        var targetExists = targetType == ReportTargetType.Post ? await CurrentTenantPosts().AnyAsync(post => post.Id == targetId, cancellationToken) : await CurrentTenantComments().AnyAsync(comment => comment.Id == targetId, cancellationToken);
        if (!targetExists)
        {
            return null;
        }

        var report = new ContentReportRecord
        {
            Id = Guid.NewGuid(),
            TargetType = targetType,
            TargetId = targetId,
            ReporterId = reporterId,
            Reason = reason.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _dbContext.ContentReports.Add(report);
        if (targetType == ReportTargetType.Post)
        {
            var post = await CurrentTenantPosts().FirstOrDefaultAsync(candidate => candidate.Id == targetId, cancellationToken);
            if (post is not null)
            {
                _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(post, "Reported"));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task<IReadOnlyList<ContentReportRecord>> GetReportsAsync(int limit, CancellationToken cancellationToken) => await CurrentTenantReports().AsNoTracking().OrderBy(report => report.Status == ReportStatus.Submitted ? 0 : 1).ThenByDescending(report => report.CreatedAtUtc).Take(limit).ToListAsync(cancellationToken);

    public async Task<ContentReportRecord?> ResolveReportAsync(Guid reportId, Guid moderatorId, string action, string auditNote, CancellationToken cancellationToken)
    {
        var report = await CurrentTenantReports().FirstOrDefaultAsync(item => item.Id == reportId, cancellationToken);
        if (report is null)
        {
            return null;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var normalizedAction = action.Trim().ToLowerInvariant();
        report.ModeratorId = moderatorId;
        report.ResolutionNote = auditNote.Trim();
        report.ResolvedAtUtc = DateTimeOffset.UtcNow;

        if (normalizedAction is "hide" or "hide_content" or "hide_comment")
        {
            report.Status = ReportStatus.ResolvedContentAction;
            await HideTargetAsync(report, cancellationToken);
        }
        else
        {
            report.Status = ReportStatus.ResolvedNoAction;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return report;
    }

    public async Task<IReadOnlyList<PostNotificationRecord>> GetNotificationsAsync(Guid recipientId, int limit, CancellationToken cancellationToken)
    {
        var query = CurrentTenantNotifications().AsNoTracking().Where(notification => notification.RecipientId == recipientId);
        return IsSqliteProvider()
            ? (await query.ToListAsync(cancellationToken)).OrderByDescending(notification => notification.CreatedAtUtc).ThenBy(notification => notification.Id).Take(limit).ToArray()
            : await query.OrderByDescending(notification => notification.CreatedAtUtc).ThenBy(notification => notification.Id).Take(limit).ToListAsync(cancellationToken);
    }

    public Task<int> CountUnreadNotificationsAsync(Guid recipientId, CancellationToken cancellationToken) =>
        CurrentTenantNotifications().CountAsync(notification => notification.RecipientId == recipientId && notification.Status == NotificationStatus.Unread, cancellationToken);

    public async Task<PostNotificationRecord?> MarkNotificationReadAsync(Guid notificationId, Guid recipientId, CancellationToken cancellationToken)
    {
        var notification = await CurrentTenantNotifications().FirstOrDefaultAsync(item => item.Id == notificationId && item.RecipientId == recipientId, cancellationToken);
        if (notification is null) return null;
        notification.MarkRead();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return notification;
    }

    public async Task RegisterMediaUploadAsync(RegisterMediaUploadCommand command, CancellationToken cancellationToken)
    {
        _dbContext.PostMediaUploads.Add(new PostMediaUploadRecord
        {
            Id = command.UploadId,
            OwnerId = command.OwnerId,
            Url = command.Url,
            ThumbnailUrl = command.ThumbnailUrl,
            ContentType = command.ContentType,
            SizeBytes = command.SizeBytes,
            CleanupDueAtUtc = command.CleanupDueAtUtc
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PostMediaUploadRecord>> ClaimMediaUploadsDueForCleanupAsync(DateTimeOffset utcNow, int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return [];
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var stalePendingUploads = IsSqliteProvider()
            ? (await CurrentTenantUploads()
                .Where(upload => upload.Status == PostMediaUploadStatus.StoredPendingAttachment
                    && upload.AttachedPostId == null)
                .ToListAsync(cancellationToken))
                .Where(upload => upload.CleanupDueAtUtc <= utcNow)
                .OrderBy(upload => upload.CleanupDueAtUtc)
                .ThenBy(upload => upload.Id)
                .Take(limit)
                .ToList()
            : await CurrentTenantUploads()
                .Where(upload => upload.Status == PostMediaUploadStatus.StoredPendingAttachment
                    && upload.AttachedPostId == null
                    && upload.CleanupDueAtUtc <= utcNow)
                .OrderBy(upload => upload.CleanupDueAtUtc)
                .ThenBy(upload => upload.Id)
                .Take(limit)
                .ToListAsync(cancellationToken);
        foreach (var upload in stalePendingUploads)
        {
            upload.Status = PostMediaUploadStatus.CleanupDue;
        }

        if (stalePendingUploads.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var uploads = IsSqliteProvider()
            ? (await CurrentTenantUploads()
                .AsNoTracking()
                .Where(upload => upload.Status == PostMediaUploadStatus.CleanupDue)
                .ToListAsync(cancellationToken))
                .OrderBy(upload => upload.CleanupDueAtUtc)
                .ThenBy(upload => upload.Id)
                .Take(limit)
                .ToList()
            : await CurrentTenantUploads()
                .AsNoTracking()
                .Where(upload => upload.Status == PostMediaUploadStatus.CleanupDue)
                .OrderBy(upload => upload.CleanupDueAtUtc)
                .ThenBy(upload => upload.Id)
                .Take(limit)
                .ToListAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return uploads;
    }

    public async Task MarkMediaUploadDeletedAsync(Guid uploadId, CancellationToken cancellationToken)
    {
        if (uploadId == Guid.Empty)
        {
            return;
        }

        var upload = await CurrentTenantUploads().FirstOrDefaultAsync(item => item.Id == uploadId, cancellationToken);
        if (upload is null || upload.Status != PostMediaUploadStatus.CleanupDue)
        {
            return;
        }

        upload.Status = PostMediaUploadStatus.Deleted;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<string>> FindAttachableMediaUploadUrlsAsync(Guid ownerId, IReadOnlyList<PostMediaUploadAttachmentRequest> mediaUploads, CancellationToken cancellationToken)
    {
        if (mediaUploads.Count == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var mediaUrls = mediaUploads.Select(upload => upload.Url).Distinct(StringComparer.Ordinal).ToArray();
        var uploads = await CurrentTenantUploads()
            .AsNoTracking()
            .Where(upload => upload.OwnerId == ownerId
                && upload.Status == PostMediaUploadStatus.StoredPendingAttachment
                && mediaUrls.Contains(upload.Url))
            .ToListAsync(cancellationToken);

        return BuildAttachableUploadUrlSet(mediaUploads, uploads);
    }

    private static IReadOnlySet<string> BuildAttachableUploadUrlSet(
        IReadOnlyList<PostMediaUploadAttachmentRequest> mediaUploads,
        IReadOnlyList<PostMediaUploadRecord> uploads)
    {
        var uploadByUrl = uploads.ToDictionary(upload => upload.Url, StringComparer.Ordinal);
        return mediaUploads
            .Where(request => uploadByUrl.TryGetValue(request.Url, out var upload)
                && string.Equals(request.ThumbnailUrl, upload.ThumbnailUrl, StringComparison.Ordinal))
            .Select(request => request.Url)
            .ToHashSet(StringComparer.Ordinal);
    }

    private async Task HideTargetAsync(ContentReportRecord report, CancellationToken cancellationToken)
    {
        if (report.TargetType == ReportTargetType.Post)
        {
            var post = await CurrentTenantPosts().FirstOrDefaultAsync(item => item.Id == report.TargetId, cancellationToken);
            if (post is not null)
            {
                post.Hide();
                _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(post, "Hidden"));
                await CurrentTenantUploads()
                    .Where(upload => upload.AttachedPostId == post.Id)
                    .ExecuteUpdateAsync(updates => updates.SetProperty(upload => upload.ModerationHold, true), cancellationToken);
            }

            return;
        }

        var comment = await CurrentTenantComments().FirstOrDefaultAsync(item => item.Id == report.TargetId, cancellationToken);
        if (comment is not null)
        {
            comment.Status = CommentStatus.Hidden;
            comment.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    private bool IsSqliteProvider() =>
        string.Equals(_dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal);

    private bool IsNpgsqlProvider() =>
        string.Equals(_dbContext.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal);

    private void AddNotification(Guid recipientId, Guid actorId, string type, string targetType, Guid targetId, Guid? postId, string message)
    {
        if (recipientId == Guid.Empty || actorId == Guid.Empty || recipientId == actorId)
        {
            return;
        }

        _dbContext.PostNotifications.Add(new PostNotificationRecord
        {
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            ActorId = actorId,
            Type = type,
            TargetType = targetType,
            TargetId = targetId,
            PostId = postId,
            Message = message,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
    }

    private IQueryable<PostRecord> CurrentTenantPosts() =>
        _dbContext.Posts.Where(post => post.TenantId == PostRecord.DefaultTenantId);

    private static IntegrationOutboxMessage CreateOutboxMessage(PostRecord post, string changeType)
    {
        var integrationEvent = PostContentChangedIntegrationEvent.Create(
            post.Id,
            post.TenantId,
            post.AuthorId,
            post.DishId,
            post.PlaceId,
            changeType);

        return IntegrationOutboxMessage.Create(
            integrationEvent,
            integrationEvent.EventId,
            integrationEvent.EventVersion,
            integrationEvent.OccurredAtUtc,
            integrationEvent.CorrelationId,
            "post.events",
            $"post.content.{changeType.ToLowerInvariant()}.v1",
            post.Id,
            "Post");
    }

    private IQueryable<PostMediaUploadRecord> CurrentTenantUploads() =>
        _dbContext.PostMediaUploads.Where(upload => upload.TenantId == PostRecord.DefaultTenantId);

    private IQueryable<PostCommentRecord> CurrentTenantComments() =>
        from comment in _dbContext.PostComments
        join post in CurrentTenantPosts() on comment.PostId equals post.Id
        select comment;

    private IQueryable<PostLikeRecord> CurrentTenantPostLikes() =>
        from like in _dbContext.PostLikes
        join post in CurrentTenantPosts() on like.PostId equals post.Id
        select like;

    private IQueryable<PostRepostRecord> CurrentTenantPostReposts() =>
        from repost in _dbContext.PostReposts
        join post in CurrentTenantPosts() on repost.PostId equals post.Id
        select repost;

    private IQueryable<PostNotificationRecord> CurrentTenantNotifications() =>
        from notification in _dbContext.PostNotifications
        where notification.PostId == null || CurrentTenantPosts().Any(post => post.Id == notification.PostId)
        select notification;

    private IQueryable<ContentReportRecord> CurrentTenantReports()
    {
        var postIds = CurrentTenantPosts().Select(post => post.Id);
        var commentIds = CurrentTenantComments().Select(comment => comment.Id);
        return _dbContext.ContentReports.Where(report =>
            (report.TargetType == ReportTargetType.Post && postIds.Contains(report.TargetId))
            || (report.TargetType == ReportTargetType.Comment && commentIds.Contains(report.TargetId)));
    }

    private IQueryable<PostRecord> ApplyTagFilter(IQueryable<PostRecord> query, string tag)
    {
        if (!IsNpgsqlProvider())
        {
            return query.Where(post => post.TagsSearchText.Contains(tag));
        }

        var pattern = $"%{EscapeLikePattern(tag)}%";
        return query.Where(post => EF.Functions.ILike(post.TagsSearchText, pattern, "\\"));
    }

    private IQueryable<PostRecord> ApplyTextSearch(IQueryable<PostRecord> query, string searchText)
    {
        if (!IsNpgsqlProvider())
        {
            return query.Where(post => post.Caption.ToLower().Contains(searchText)
                || post.TagsSearchText.ToLower().Contains(searchText)
                || (post.LocationText != null && post.LocationText.ToLower().Contains(searchText)));
        }

        return query.Where(post =>
            EF.Functions.ToTsVector("simple", post.Caption + " " + post.TagsSearchText + " " + (post.LocationText ?? string.Empty))
                .Matches(EF.Functions.PlainToTsQuery("simple", searchText))
            || EF.Functions.TrigramsAreSimilar(post.Caption, searchText)
            || EF.Functions.TrigramsAreSimilar(post.TagsSearchText, searchText)
            || (post.LocationText != null && EF.Functions.TrigramsAreSimilar(post.LocationText, searchText)));
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private static IQueryable<PostRecord> IncludePostGraph(IQueryable<PostRecord> posts) => posts.Include(post => post.Media).Include(post => post.Likes).Include(post => post.Comments).Include(post => post.Reposts);

    private async Task<IReadOnlyList<PostRecord>> LoadPostsByIdsAsync(IReadOnlyList<Guid> postIds, CancellationToken cancellationToken)
    {
        if (postIds.Count == 0) return [];
        var order = postIds.Select((id, index) => new { id, index }).ToDictionary(item => item.id, item => item.index);
        var posts = await IncludePostGraph(CurrentTenantPosts()).AsSplitQuery().Where(post => postIds.Contains(post.Id)).ToListAsync(cancellationToken);
        return posts.OrderBy(post => order[post.Id]).ToArray();
    }
}
