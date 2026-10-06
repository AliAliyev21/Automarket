using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoMarket.BuildingBlocks.Audit.Configurations;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(100);
        builder.Property(x => x.TargetType).HasMaxLength(50);
        builder.Property(x => x.TargetId).HasMaxLength(100);
        builder.Property(x => x.Result).HasMaxLength(50);
        builder.Property(x => x.Ip).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.CorrelationId).HasMaxLength(64);
        builder.Property(x => x.Details).HasColumnType("jsonb");
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => new { x.EventType, x.OccurredAt });
        builder.HasIndex(x => new { x.ActorId, x.OccurredAt });
    }
}
