using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BillingSystem.Domain;

namespace BillingSystem.Infrastructure.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "billing");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.EntityName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(x => x.OldValue);

        builder.Property(x => x.NewValue);

        builder.Property(x => x.PerformedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.HasIndex(x => new { x.EntityName, x.EntityId, x.OccurredAt })
            .HasDatabaseName("IX_AuditLogs_Entity_EntityId_OccurredAt");

        builder.HasIndex(x => new { x.Action, x.OccurredAt })
            .HasDatabaseName("IX_AuditLogs_Action_OccurredAt");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_AuditLogs_CorrelationId");
    }
}