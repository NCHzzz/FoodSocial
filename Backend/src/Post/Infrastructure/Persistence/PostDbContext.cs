using FoodSocial.Post.Domain;
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
    public DbSet<ContentReportRecord> ContentReports => Set<ContentReportRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<PostRecord>(entity =>
        {
            entity.ToTable("posts"); entity.HasKey(post => post.Id);
            entity.Property(post => post.Caption).HasMaxLength(2000).IsRequired();
            entity.Property(post => post.PriceRange).HasMaxLength(16);
            entity.Property(post => post.Tags).HasConversion(MultiValueListConverter, MultiValueListComparer);
            entity.Property(post => post.TagsSearchText).HasColumnName("tags_search_text").HasMaxLength(1000);
            entity.Property(post => post.LocationText).HasMaxLength(500);
            entity.Property(post => post.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasMany(post => post.Media).WithOne().HasForeignKey(media => media.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(post => post.Likes).WithOne().HasForeignKey(like => like.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(post => post.Comments).WithOne().HasForeignKey(comment => comment.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(post => post.AuthorId); entity.HasIndex(post => post.DishId); entity.HasIndex(post => post.PlaceId); entity.HasIndex(post => post.Status); entity.HasIndex(post => post.CreatedAtUtc);
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
        modelBuilder.Entity<ContentReportRecord>(entity =>
        {
            entity.ToTable("content_reports"); entity.HasKey(report => report.Id); entity.Property(report => report.TargetType).HasConversion<string>().HasMaxLength(32).IsRequired(); entity.Property(report => report.Reason).HasMaxLength(80).IsRequired(); entity.Property(report => report.Description).HasMaxLength(1000); entity.Property(report => report.Status).HasConversion<string>().HasMaxLength(64).IsRequired(); entity.Property(report => report.ResolutionNote).HasMaxLength(1000); entity.HasIndex(report => new { report.TargetType, report.TargetId }); entity.HasIndex(report => report.Status); entity.HasIndex(report => report.CreatedAtUtc);
        });
    }

    private static string SerializeMultiValueList(IReadOnlyCollection<string> values) => values.Count == 0 ? string.Empty : string.Join(MultiValueSeparator, values);
    private static List<string> DeserializeMultiValueList(string? value) => string.IsNullOrWhiteSpace(value) ? [] : value.Split(MultiValueSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    private static bool ListsAreEqual(IReadOnlyList<string>? left, IReadOnlyList<string>? right) => ReferenceEquals(left, right) || (left is not null && right is not null && left.SequenceEqual(right, StringComparer.Ordinal));
    private static int GetListHashCode(IReadOnlyList<string>? values) { if (values is null) return 0; var hash = new HashCode(); foreach (var value in values) hash.Add(value, StringComparer.Ordinal); return hash.ToHashCode(); }
}
