using AutoMarket.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;

namespace AutoMarket.BuildingBlocks.Persistence;

// Outbox və inbox hər modulun öz schema-sında eyni strukturla saxlanılır (ARCHITECTURE §5.2, §5.3, §4.5)
public static class MessagingModelBuilderExtensions
{
    public static ModelBuilder ConfigureMessagingTables(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).HasMaxLength(200);
            builder.Property(x => x.Payload).HasColumnType("jsonb");
            builder.Property(x => x.CorrelationId).HasMaxLength(64);
            builder.Property(x => x.TraceParent).HasMaxLength(128);
            builder.Property(x => x.LastError).HasMaxLength(1000);
            builder.HasIndex(x => x.OccurredAt).HasFilter("processed_at IS NULL");
            builder.HasIndex(x => x.ProcessedAt);
        });

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("inbox");
            builder.HasKey(x => new { x.MessageId, x.Consumer });
            builder.Property(x => x.Consumer).HasMaxLength(100);
        });

        return modelBuilder;
    }
}
