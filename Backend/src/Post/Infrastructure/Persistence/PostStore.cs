using FoodSocial.Post.Application;
using FoodSocial.Post.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodSocial.Post.Infrastructure.Persistence;

public sealed class PostStore(PostDbContext dbContext) : IPostStore
{
    private readonly PostDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<PostRecord> CreatePostAsync(PostRecord post, CancellationToken cancellationToken)
    {
        _dbContext.Posts.Add(post); await _dbContext.SaveChangesAsync(cancellationToken); return post;
    }

    public async Task<PostRecord?> GetPostAsync(Guid postId, CancellationToken cancellationToken) => await IncludePostGraph(_dbContext.Posts).AsSplitQuery().FirstOrDefaultAsync(post => post.Id == postId, cancellationToken);

    public async Task<IReadOnlyList<PostRecord>> SearchPostsAsync(PostSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _dbContext.Posts.AsQueryable();
        if (!criteria.IncludeHidden) query = query.Where(post => post.Status == PostStatus.Published);
        if (criteria.AuthorId is { } authorId) query = query.Where(post => post.AuthorId == authorId);
        if (criteria.DishId is { } dishId) query = query.Where(post => post.DishId == dishId);
        if (criteria.PlaceId is { } placeId) query = query.Where(post => post.PlaceId == placeId);
        if (!string.IsNullOrWhiteSpace(criteria.Tag)) query = query.Where(post => post.TagsSearchText.ToLower().Contains(criteria.Tag));
        if (!string.IsNullOrWhiteSpace(criteria.Query)) query = query.Where(post => post.Caption.ToLower().Contains(criteria.Query) || post.TagsSearchText.ToLower().Contains(criteria.Query) || (post.LocationText != null && post.LocationText.ToLower().Contains(criteria.Query)));
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
        var publishedPosts = _dbContext.Posts
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
        var exists = await _dbContext.Posts.AnyAsync(post => post.Id == postId, cancellationToken); if (!exists) return null;
        var alreadyLiked = await _dbContext.PostLikes.AnyAsync(like => like.PostId == postId && like.ActorId == actorId, cancellationToken);
        if (!alreadyLiked) { _dbContext.PostLikes.Add(new PostLikeRecord { PostId = postId, ActorId = actorId }); await _dbContext.SaveChangesAsync(cancellationToken); }
        return await _dbContext.PostLikes.CountAsync(like => like.PostId == postId, cancellationToken);
    }

    public async Task<int?> RemoveLikeAsync(Guid postId, Guid actorId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Posts.AnyAsync(post => post.Id == postId, cancellationToken); if (!exists) return null;
        var like = await _dbContext.PostLikes.FindAsync([postId, actorId], cancellationToken); if (like is not null) { _dbContext.PostLikes.Remove(like); await _dbContext.SaveChangesAsync(cancellationToken); }
        return await _dbContext.PostLikes.CountAsync(item => item.PostId == postId, cancellationToken);
    }

    public async Task<PostCommentRecord?> AddCommentAsync(Guid postId, Guid authorId, string body, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Posts.AnyAsync(post => post.Id == postId, cancellationToken); if (!exists) return null;
        var comment = new PostCommentRecord { Id = Guid.NewGuid(), PostId = postId, AuthorId = authorId, Body = body.Trim(), CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow };
        _dbContext.PostComments.Add(comment); await _dbContext.SaveChangesAsync(cancellationToken); return comment;
    }

    public async Task<PostCommentRecord?> GetCommentAsync(Guid commentId, CancellationToken cancellationToken) => await _dbContext.PostComments.FirstOrDefaultAsync(comment => comment.Id == commentId, cancellationToken);

    public async Task<PostCommentRecord?> SoftDeleteCommentAsync(Guid commentId, Guid actorId, CancellationToken cancellationToken)
    {
        var comment = await GetCommentAsync(commentId, cancellationToken); if (comment is null || comment.AuthorId != actorId) return null;
        comment.Status = CommentStatus.Deleted; comment.UpdatedAtUtc = DateTimeOffset.UtcNow; await _dbContext.SaveChangesAsync(cancellationToken); return comment;
    }

    public async Task<ContentReportRecord?> CreateReportAsync(ReportTargetType targetType, Guid targetId, Guid reporterId, string reason, string? description, CancellationToken cancellationToken)
    {
        var targetExists = targetType == ReportTargetType.Post ? await _dbContext.Posts.AnyAsync(post => post.Id == targetId, cancellationToken) : await _dbContext.PostComments.AnyAsync(comment => comment.Id == targetId, cancellationToken);
        if (!targetExists) return null;
        var report = new ContentReportRecord { Id = Guid.NewGuid(), TargetType = targetType, TargetId = targetId, ReporterId = reporterId, Reason = reason.Trim(), Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(), CreatedAtUtc = DateTimeOffset.UtcNow };
        _dbContext.ContentReports.Add(report); await _dbContext.SaveChangesAsync(cancellationToken); return report;
    }

    public async Task<IReadOnlyList<ContentReportRecord>> GetReportsAsync(int limit, CancellationToken cancellationToken) => await _dbContext.ContentReports.AsNoTracking().OrderBy(report => report.Status == ReportStatus.Submitted ? 0 : 1).ThenByDescending(report => report.CreatedAtUtc).Take(limit).ToListAsync(cancellationToken);

    public async Task<ContentReportRecord?> ResolveReportAsync(Guid reportId, Guid moderatorId, string action, string auditNote, CancellationToken cancellationToken)
    {
        var report = await _dbContext.ContentReports.FirstOrDefaultAsync(item => item.Id == reportId, cancellationToken); if (report is null) return null;
        var normalizedAction = action.Trim().ToLowerInvariant();
        report.ModeratorId = moderatorId; report.ResolutionNote = auditNote.Trim(); report.ResolvedAtUtc = DateTimeOffset.UtcNow;
        if (normalizedAction is "hide" or "hide_content" or "hide_comment") { report.Status = ReportStatus.ResolvedContentAction; await HideTargetAsync(report, cancellationToken); } else { report.Status = ReportStatus.ResolvedNoAction; }
        await _dbContext.SaveChangesAsync(cancellationToken); return report;
    }

    private async Task HideTargetAsync(ContentReportRecord report, CancellationToken cancellationToken)
    {
        if (report.TargetType == ReportTargetType.Post) { var post = await _dbContext.Posts.FirstOrDefaultAsync(item => item.Id == report.TargetId, cancellationToken); post?.Hide(); return; }
        var comment = await _dbContext.PostComments.FirstOrDefaultAsync(item => item.Id == report.TargetId, cancellationToken); if (comment is not null) { comment.Status = CommentStatus.Hidden; comment.UpdatedAtUtc = DateTimeOffset.UtcNow; }
    }

    private bool IsSqliteProvider() =>
        string.Equals(_dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal);

    private static IQueryable<PostRecord> IncludePostGraph(IQueryable<PostRecord> posts) => posts.Include(post => post.Media).Include(post => post.Likes).Include(post => post.Comments);

    private async Task<IReadOnlyList<PostRecord>> LoadPostsByIdsAsync(IReadOnlyList<Guid> postIds, CancellationToken cancellationToken)
    {
        if (postIds.Count == 0) return [];
        var order = postIds.Select((id, index) => new { id, index }).ToDictionary(item => item.id, item => item.index);
        var posts = await IncludePostGraph(_dbContext.Posts).AsSplitQuery().Where(post => postIds.Contains(post.Id)).ToListAsync(cancellationToken);
        return posts.OrderBy(post => order[post.Id]).ToArray();
    }
}
