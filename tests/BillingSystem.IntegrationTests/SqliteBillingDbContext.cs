using BillingSystem.Domain;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.IntegrationTests;

/// <summary>
/// SQLite-compatible variant of <see cref="BillingDbContext"/> for integration tests.
/// SQL Server generates <c>rowversion</c> server-side, which SQLite cannot do, so the
/// concurrency token is disabled and the value is treated as a plain column.
/// </summary>
public sealed class SqliteBillingDbContext : BillingDbContext
{
    public SqliteBillingDbContext(DbContextOptions<BillingDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var rowVersion = modelBuilder.Entity<Subscription>().Property(x => x.RowVersion);
        rowVersion.ValueGeneratedNever();
        rowVersion.IsConcurrencyToken(false);
    }
}
