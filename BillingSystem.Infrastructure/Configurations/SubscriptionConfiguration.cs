using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BillingSystem.Domain;

namespace BillingSystem.Infrastructure.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions", "billing");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CustomerId)
            .IsRequired();

        builder.Property(x => x.PlanId)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(x => x.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.NextBillingDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.EndDate)
            .HasColumnType("date");

        builder.Property(x => x.CancelledAt)
            .HasColumnType("datetime2(3)");

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.HasIndex(x => new { x.Status, x.NextBillingDate })
            .HasDatabaseName("IX_Subscriptions_Due");

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Plan)
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}