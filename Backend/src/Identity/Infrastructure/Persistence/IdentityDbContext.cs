using FoodSocial.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodSocial.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<IdentityUser> IdentityUsers => Set<IdentityUser>();
    public DbSet<IdentityRoleAssignment> IdentityRoleAssignments => Set<IdentityRoleAssignment>();
    public DbSet<IdentityOutboxMessage> IdentityOutboxMessages => Set<IdentityOutboxMessage>();

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

            entity.Property(user => user.State)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.HasIndex(user => user.Subject)
                .IsUnique();
        });

        modelBuilder.Entity<IdentityRoleAssignment>(entity =>
        {
            entity.ToTable("identity_role_assignments");

            entity.HasKey(assignment => assignment.Id);

            entity.Property(assignment => assignment.Subject)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(assignment => assignment.Role)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(assignment => assignment.AssignedAtUtc)
                .IsRequired();

            entity.HasIndex(assignment => new { assignment.Subject, assignment.Role })
                .IsUnique();
        });

        modelBuilder.Entity<IdentityOutboxMessage>(entity =>
        {
            entity.ToTable("identity_outbox_messages");

            entity.HasKey(message => message.Id);

            entity.Property(message => message.EventType)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(message => message.EventVersion)
                .IsRequired();

            entity.Property(message => message.IdentityId)
                .IsRequired();

            entity.Property(message => message.Subject)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(message => message.PayloadJson)
                .HasColumnType("text")
                .IsRequired();

            entity.Property(message => message.DeliveryState)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(message => message.AttemptCount)
                .IsRequired();

            entity.Property(message => message.CreatedAtUtc)
                .IsRequired();

            entity.Property(message => message.LastAttemptAtUtc);

            entity.Property(message => message.DeliveredAtUtc);

            entity.Property(message => message.LastError)
                .HasMaxLength(2000);

            entity.HasIndex(message => new { message.DeliveryState, message.CreatedAtUtc });
            entity.HasIndex(message => message.Subject);
        });
    }
}
