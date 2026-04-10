using FoodSocial.UserProfile.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodSocial.UserProfile.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for the UserProfile boundary.
/// </summary>
public sealed class UserProfileDbContext(DbContextOptions<UserProfileDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the persisted profile shells.
    /// </summary>
    public DbSet<UserProfileShell> UserProfileShells => Set<UserProfileShell>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserProfileShell>(entity =>
        {
            entity.ToTable("user_profile_shells");

            entity.HasKey(shell => shell.Id);

            entity.Property(shell => shell.IdentityId)
                .IsRequired();

            entity.Property(shell => shell.Subject)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(shell => shell.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(shell => shell.IdentityId)
                .IsUnique();

            entity.HasIndex(shell => shell.Subject)
                .IsUnique();
        });
    }
}
