using FoodSocial.UserProfile.Domain;
using FoodSocial.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FoodSocial.UserProfile.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for the UserProfile boundary.
/// </summary>
public sealed class UserProfileDbContext(DbContextOptions<UserProfileDbContext> options) : DbContext(options)
{
    private const char MultiValueSeparator = '|';
    private static readonly ValueConverter<IReadOnlyList<string>, string> MultiValueListConverter = new(
        values => SerializeMultiValueList(values),
        value => DeserializeMultiValueList(value));

    private static readonly ValueComparer<IReadOnlyList<string>> MultiValueListComparer = new(
        (left, right) => ListsAreEqual(left, right),
        values => GetListHashCode(values),
        values => values.ToList());

    /// <summary>
    /// Gets the persisted profile shells.
    /// </summary>
    public DbSet<UserProfileShell> UserProfileShells => Set<UserProfileShell>();

    /// <summary>
    /// Gets the persisted application-owned profile details.
    /// </summary>
    public DbSet<UserProfileDetail> UserProfileDetails => Set<UserProfileDetail>();

    /// <summary>
    /// Gets the persisted follow relationships.
    /// </summary>
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();

    /// <summary>
    /// Gets the persisted saved-post relationships.
    /// </summary>
    public DbSet<SavedPost> SavedPosts => Set<SavedPost>();

    /// <summary>
    /// Gets the persisted saved-dish relationships.
    /// </summary>
    public DbSet<SavedDish> SavedDishes => Set<SavedDish>();

    /// <summary>
    /// Gets the persisted saved-place relationships.
    /// </summary>
    public DbSet<SavedPlace> SavedPlaces => Set<SavedPlace>();

    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<UserSafetyRelation> UserSafetyRelations => Set<UserSafetyRelation>();
public DbSet<IntegrationOutboxMessage> IntegrationOutboxMessages => Set<IntegrationOutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureIntegrationOutbox("userprofile_outbox_messages");

        modelBuilder.Entity<UserProfileShell>(entity =>
        {
            entity.ToTable("user_profile_shells");

            entity.HasKey(shell => shell.Id);

            entity.Property(shell => shell.IdentityId)
                .IsRequired();

            entity.Property(shell => shell.TenantId)
                .IsRequired();

            entity.Property(shell => shell.Subject)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(shell => shell.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(shell => new { shell.TenantId, shell.IdentityId })
                .IsUnique();

            entity.HasIndex(shell => new { shell.TenantId, shell.Subject })
                .IsUnique();

            entity.HasOne(shell => shell.Detail)
                .WithOne()
                .HasForeignKey<UserProfileDetail>(detail => detail.UserProfileShellId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserProfileDetail>(entity =>
        {
            entity.ToTable("user_profile_details");

            entity.HasKey(detail => detail.Id);

            entity.Property(detail => detail.UserProfileShellId)
                .IsRequired();

            entity.Property(detail => detail.DisplayName)
                .HasMaxLength(100);

            entity.Property(detail => detail.AvatarUrl)
                .HasMaxLength(500);

            entity.Property(detail => detail.Bio)
                .HasMaxLength(1000);

            entity.Property(detail => detail.CreatedAtUtc)
                .IsRequired();

            entity.Property(detail => detail.UpdatedAtUtc)
                .IsRequired();

            entity.Property(detail => detail.PreferredCuisines)
                .HasConversion(MultiValueListConverter, MultiValueListComparer);

            entity.Property(detail => detail.DislikedIngredients)
                .HasConversion(MultiValueListConverter, MultiValueListComparer);

            entity.Property(detail => detail.DietaryRestrictions)
                .HasConversion(MultiValueListConverter, MultiValueListComparer);

            entity.HasIndex(detail => detail.UserProfileShellId)
                .IsUnique();
        });

        modelBuilder.Entity<UserFollow>(entity =>
        {
            entity.ToTable("user_follows");

            entity.HasKey(follow => follow.Id);

            entity.Property(follow => follow.FollowerUserProfileShellId)
                .IsRequired();

            entity.Property(follow => follow.FollowedUserProfileShellId)
                .IsRequired();

            entity.Property(follow => follow.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(follow => new { follow.FollowerUserProfileShellId, follow.FollowedUserProfileShellId })
                .IsUnique();
            entity.HasIndex(follow => new { follow.FollowedUserProfileShellId, follow.CreatedAtUtc });
            entity.HasIndex(follow => new { follow.FollowerUserProfileShellId, follow.CreatedAtUtc });

            entity.HasOne<UserProfileShell>()
                .WithMany()
                .HasForeignKey(follow => follow.FollowerUserProfileShellId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<UserProfileShell>()
                .WithMany()
                .HasForeignKey(follow => follow.FollowedUserProfileShellId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SavedPost>(entity =>
        {
            entity.ToTable("saved_posts");

            entity.HasKey(savedPost => savedPost.Id);

            entity.Property(savedPost => savedPost.UserProfileShellId)
                .IsRequired();

            entity.Property(savedPost => savedPost.PostId)
                .IsRequired();

            entity.Property(savedPost => savedPost.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(savedPost => new { savedPost.UserProfileShellId, savedPost.PostId })
                .IsUnique();
            entity.HasIndex(savedPost => new { savedPost.UserProfileShellId, savedPost.CreatedAtUtc });

            entity.HasOne<UserProfileShell>()
                .WithMany()
                .HasForeignKey(savedPost => savedPost.UserProfileShellId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SavedDish>(entity =>
        {
            entity.ToTable("saved_dishes");

            entity.HasKey(savedDish => savedDish.Id);

            entity.Property(savedDish => savedDish.UserProfileShellId)
                .IsRequired();

            entity.Property(savedDish => savedDish.DishId)
                .IsRequired();

            entity.Property(savedDish => savedDish.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(savedDish => new { savedDish.UserProfileShellId, savedDish.DishId })
                .IsUnique();
            entity.HasIndex(savedDish => new { savedDish.UserProfileShellId, savedDish.CreatedAtUtc });

            entity.HasOne<UserProfileShell>()
                .WithMany()
                .HasForeignKey(savedDish => savedDish.UserProfileShellId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SavedPlace>(entity =>
        {
            entity.ToTable("saved_places");

            entity.HasKey(savedPlace => savedPlace.Id);

            entity.Property(savedPlace => savedPlace.UserProfileShellId)
                .IsRequired();

            entity.Property(savedPlace => savedPlace.PlaceId)
                .IsRequired();

            entity.Property(savedPlace => savedPlace.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(savedPlace => new { savedPlace.UserProfileShellId, savedPlace.PlaceId })
                .IsUnique();
            entity.HasIndex(savedPlace => new { savedPlace.UserProfileShellId, savedPlace.CreatedAtUtc });

            entity.HasOne<UserProfileShell>()
                .WithMany()
                .HasForeignKey(savedPlace => savedPlace.UserProfileShellId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.ToTable("user_notifications");
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.UserProfileShellId).IsRequired();
            entity.Property(notification => notification.RecipientIdentityId).IsRequired();
            entity.Property(notification => notification.ActorIdentityId).IsRequired();
            entity.Property(notification => notification.Type).HasMaxLength(64).IsRequired();
            entity.Property(notification => notification.Message).HasMaxLength(280).IsRequired();
            entity.Property(notification => notification.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(notification => notification.CreatedAtUtc).IsRequired();
            entity.HasIndex(notification => notification.RecipientIdentityId);
            entity.HasIndex(notification => new { notification.RecipientIdentityId, notification.Status });
            entity.HasIndex(notification => new { notification.UserProfileShellId, notification.CreatedAtUtc });
            entity.HasIndex(notification => new { notification.UserProfileShellId, notification.Status });
            entity.HasIndex(notification => notification.CreatedAtUtc);
            entity.HasOne<UserProfileShell>().WithMany().HasForeignKey(notification => notification.UserProfileShellId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UserSafetyRelation>(entity =>
        {
            entity.ToTable("user_safety_relations");
            entity.HasKey(relation => relation.Id);

            entity.Property(relation => relation.TenantId)
                .IsRequired();

            entity.Property(relation => relation.ActorIdentityId)
                .IsRequired();

            entity.Property(relation => relation.TargetIdentityId)
                .IsRequired();

            entity.Property(relation => relation.Type)
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();

            entity.Property(relation => relation.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(relation => new { relation.ActorIdentityId, relation.TargetIdentityId, relation.Type })
                .IsUnique();

            entity.HasIndex(relation => new { relation.ActorIdentityId, relation.Type });

            entity.HasIndex(relation => new { relation.TargetIdentityId, relation.Type });
        });
    }


    private static string SerializeMultiValueList(IReadOnlyCollection<string> values) =>
        values.Count == 0
            ? string.Empty
            : string.Join(MultiValueSeparator, values);

    private static List<string> DeserializeMultiValueList(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(MultiValueSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static bool ListsAreEqual(IReadOnlyList<string>? left, IReadOnlyList<string>? right) =>
        ReferenceEquals(left, right)
        || (left is not null
            && right is not null
            && left.SequenceEqual(right, StringComparer.Ordinal));

    private static int GetListHashCode(IReadOnlyList<string>? values)
    {
        if (values is null)
        {
            return 0;
        }

        var hash = new HashCode();
        foreach (var value in values)
        {
            hash.Add(value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
