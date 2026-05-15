using FoodSocial.Post.Domain;
using FoodSocial.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FoodSocial.Post.Infrastructure.Persistence;

public sealed class PostDbContext(DbContextOptions<PostDbContext> options) : DbContext(options)
{
    private const char MultiValueSeparator = '|';
    private static readonly ValueConverter<List<string>, string> MultiValueListConverter = new(values => SerializeMultiValueList(values), value => DeserializeMultiValueList(value));
    private static readonly ValueComparer<List<string>> MultiValueListComparer = new((left, right) => ListsAreEqual(left, right), values => GetListHashCode(values), values => values.ToList());

    public DbSet<PostRecord> Posts => Set<PostRecord>();
    public DbSet<PostMediaRecord> PostMedia => Set<PostMediaRecord>();
    public DbSet<PostLikeRecord> PostLikes => Set<PostLikeRecord>();
    public DbSet<PostCommentRecord> PostComments => Set<PostCommentRecord>();
    public DbSet<PostRepostRecord> PostReposts => Set<PostRepostRecord>();
    public DbSet<ContentReportRecord> ContentReports => Set<ContentReportRecord>();
    public DbSet<PostNotificationRecord> PostNotifications => Set<PostNotificationRecord>();
    public DbSet<PostMediaUploadRecord> PostMediaUploads => Set<PostMediaUploadRecord>();
    public DbSet<IntegrationOutboxMessage> IntegrationOutboxMessages => Set<IntegrationOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ConfigureIntegrationOutbox("post_outbox_messages");
        modelBuilder.Entity<PostRecord>(entity =>
        {
            entity.ToTable("posts");
            entity.HasKey(post => post.Id);
            entity.Property(post => post.TenantId).IsRequired();
            entity.Property(post => post.Caption).HasMaxLength(2000).IsRequired();
            entity.Property(post => post.PriceRange).HasMaxLength(16);
            entity.Property(post => post.Tags).HasConversion(MultiValueListConverter, MultiValueListComparer);
            entity.Property(post => post.TagsSearchText).HasColumnName("tags_search_text").HasMaxLength(1000);
            entity.Property(post => post.LocationText).HasMaxLength(500);
            entity.Property(post => post.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasMany(post => post.Media).WithOne().HasForeignKey(media => media.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(post => post.Likes).WithOne().HasForeignKey(like => like.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(post => post.Comments).WithOne().HasForeignKey(comment => comment.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(post => post.Reposts).WithOne().HasForeignKey(repost => repost.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(post => new { post.TenantId, post.AuthorId, post.CreatedAtUtc });
            entity.HasIndex(post => new { post.TenantId, post.DishId, post.CreatedAtUtc });
            entity.HasIndex(post => new { post.TenantId, post.PlaceId, post.CreatedAtUtc });
            entity.HasIndex(post => new { post.TenantId, post.Status, post.CreatedAtUtc });
            entity.HasIndex(post => new { post.TenantId, post.CreatedAtUtc });
        });
        modelBuilder.Entity<PostMediaRecord>(entity =>
        {
            entity.ToTable("post_media"); entity.HasKey(media => media.Id);
            entity.Property(media => media.Url).HasMaxLength(2048).IsRequired(); entity.Property(media => media.Type).HasMaxLength(32).IsRequired(); entity.Property(media => media.ThumbnailUrl).HasMaxLength(2048); entity.Property(media => media.Caption).HasMaxLength(500);
            entity.HasIndex(media => new { media.PostId, media.IsPrimary });
        });
        modelBuilder.Entity<PostLikeRecord>(entity =>
        {
            entity.ToTable("post_likes"); entity.HasKey(like => new { like.PostId, like.ActorId }); entity.HasIndex(like => like.ActorId);
        });
        modelBuilder.Entity<PostCommentRecord>(entity =>
        {
            entity.ToTable("post_comments"); entity.HasKey(comment => comment.Id); entity.Property(comment => comment.Body).HasMaxLength(2000).IsRequired(); entity.Property(comment => comment.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); entity.HasIndex(comment => comment.PostId); entity.HasIndex(comment => comment.AuthorId); entity.HasIndex(comment => comment.Status);
        });
        modelBuilder.Entity<PostRepostRecord>(entity =>
        {
            entity.ToTable("post_reposts");
            entity.HasKey(repost => repost.Id);
            entity.Property(repost => repost.Caption).HasMaxLength(280);
            entity.HasIndex(repost => new { repost.PostId, repost.ActorId }).IsUnique();
            entity.HasIndex(repost => repost.ActorId);
            entity.HasIndex(repost => repost.CreatedAtUtc);
        });
        modelBuilder.Entity<ContentReportRecord>(entity =>
        {
            entity.ToTable("content_reports"); entity.HasKey(report => report.Id); entity.Property(report => report.TargetType).HasConversion<string>().HasMaxLength(32).IsRequired(); entity.Property(report => report.Reason).HasMaxLength(80).IsRequired(); entity.Property(report => report.Description).HasMaxLength(1000); entity.Property(report => report.Status).HasConversion<string>().HasMaxLength(64).IsRequired(); entity.Property(report => report.ResolutionNote).HasMaxLength(1000); entity.HasIndex(report => new { report.TargetType, report.TargetId }); entity.HasIndex(report => report.Status); entity.HasIndex(report => report.CreatedAtUtc);
        });
        modelBuilder.Entity<PostNotificationRecord>(entity =>
        {
            entity.ToTable("post_notifications");
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Type).HasMaxLength(64).IsRequired();
            entity.Property(notification => notification.TargetType).HasMaxLength(64).IsRequired();
            entity.Property(notification => notification.Message).HasMaxLength(280).IsRequired();
            entity.Property(notification => notification.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(notification => notification.RecipientId);
            entity.HasIndex(notification => new { notification.RecipientId, notification.Status });
            entity.HasIndex(notification => notification.CreatedAtUtc);
        });
        modelBuilder.Entity<PostMediaUploadRecord>(entity =>
        {
            entity.ToTable("post_media_uploads");
            entity.HasKey(upload => upload.Id);
            entity.Property(upload => upload.TenantId).IsRequired();
            entity.Property(upload => upload.Url).HasMaxLength(2048).IsRequired();
            entity.Property(upload => upload.ThumbnailUrl).HasMaxLength(2048).IsRequired();
            entity.Property(upload => upload.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(upload => upload.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            entity.HasIndex(upload => new { upload.TenantId, upload.OwnerId });
            entity.HasIndex(upload => upload.Url).IsUnique();
            entity.HasIndex(upload => new { upload.TenantId, upload.Status });
            entity.HasIndex(upload => new { upload.TenantId, upload.CleanupDueAtUtc });
            entity.HasOne<PostRecord>().WithMany().HasForeignKey(upload => upload.AttachedPostId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static string SerializeMultiValueList(IReadOnlyCollection<string> values) => values.Count == 0 ? string.Empty : string.Join(MultiValueSeparator, values);
    private static List<string> DeserializeMultiValueList(string? value) => string.IsNullOrWhiteSpace(value) ? [] : value.Split(MultiValueSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    private static bool ListsAreEqual(IReadOnlyList<string>? left, IReadOnlyList<string>? right) => ReferenceEquals(left, right) || (left is not null && right is not null && left.SequenceEqual(right, StringComparer.Ordinal));
    private static int GetListHashCode(IReadOnlyList<string>? values) { if (values is null) return 0; var hash = new HashCode(); foreach (var value in values) hash.Add(value, StringComparer.Ordinal); return hash.ToHashCode(); }
}
