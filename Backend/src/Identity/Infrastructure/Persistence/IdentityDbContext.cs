using FoodSocial.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodSocial.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<IdentityUser> IdentityUsers => Set<IdentityUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityUser>(entity =>
        {
            entity.ToTable("identity_users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Subject)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(user => user.PreferredUsername)
                .HasMaxLength(100);

            entity.Property(user => user.Email)
                .HasMaxLength(320);

            entity.Property(user => user.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(user => user.Subject)
                .IsUnique();
        });
    }
}
