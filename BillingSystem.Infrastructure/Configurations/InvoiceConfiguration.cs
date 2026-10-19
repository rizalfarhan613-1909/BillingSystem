using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BillingSystem.Domain;

namespace BillingSystem.Infrastructure.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices", "billing");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.InvoiceNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.SubscriptionId)
            .IsRequired();

        builder.Property(x => x.CustomerId)
            .IsRequired();

        builder.Property(x => x.BillingPeriodStart)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.BillingPeriodEnd)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(x => x.IssueDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.DueDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.PaidAt)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.HasIndex(x => x.InvoiceNumber)
            .IsUnique()
            .HasDatabaseName("UX_Invoices_InvoiceNumber");

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_Invoices_IdempotencyKey");

        builder.HasIndex(x => new { x.SubscriptionId, x.BillingPeriodStart })
            .IsUnique()
            .HasDatabaseName("UX_Invoices_Subscription_Period");

        builder.HasIndex(x => new { x.Status, x.DueDate })
            .HasDatabaseName("IX_Invoices_Status_DueDate");

        builder.HasOne(x => x.Subscription)
            .WithMany(x => x.Invoices)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}