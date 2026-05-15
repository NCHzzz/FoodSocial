using Microsoft.EntityFrameworkCore;

namespace FoodSocial.Messaging;

public static class IntegrationOutboxModelBuilderExtensions
{
    public static void ConfigureIntegrationOutbox(this ModelBuilder modelBuilder, string tableName)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        modelBuilder.Entity<IntegrationOutboxMessage>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey(message => message.Id);
            entity.Property(message => message.EventId).IsRequired();
            entity.Property(message => message.EventType).HasMaxLength(200).IsRequired();
            entity.Property(message => message.EventVersion).IsRequired();
            entity.Property(message => message.ExchangeName).HasMaxLength(200).IsRequired();
            entity.Property(message => message.RoutingKey).HasMaxLength(200).IsRequired();
            entity.Property(message => message.AggregateId).IsRequired();
            entity.Property(message => message.AggregateType).HasMaxLength(100).IsRequired();
            entity.Property(message => message.CorrelationId).HasMaxLength(200);
            entity.Property(message => message.OccurredAtUtc).IsRequired();
            entity.Property(message => message.PayloadJson).IsRequired();
            entity.Property(message => message.DeliveryState).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(message => message.CreatedAtUtc).IsRequired();
            entity.Property(message => message.LastError).HasMaxLength(2000);
            entity.HasIndex(message => message.EventId).IsUnique();
            entity.HasIndex(message => new { message.DeliveryState, message.AttemptCount, message.CreatedAtUtc });
        });
    }
}
