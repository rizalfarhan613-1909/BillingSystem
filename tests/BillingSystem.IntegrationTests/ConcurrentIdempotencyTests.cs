using BillingSystem.Application;
using BillingSystem.Domain;
using BillingSystem.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BillingSystem.IntegrationTests;

public sealed class ConcurrentIdempotencyTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public ConcurrentIdempotencyTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private SqliteBillingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseSqlite(_connection)
            .Options;
        var context = new SqliteBillingDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task ParallelIdenticalInserts_ResultsInSingleInvoice()
    {
        var subscriptionId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var periodStart = new DateOnly(2026, 1, 1);
        var periodEnd = new DateOnly(2026, 1, 31);
        var key = IdempotencyKeyFactory.Create(subscriptionId, periodStart);

        await using (var seed = CreateContext())
        {
            seed.Customers.Add(new Customer
            {
                Id = customerId,
                FullName = "Concurrency Test",
                Email = "concurrency@example.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            seed.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Id = planId,
                Name = "Basic",
                Price = 100000m,
                Currency = "IDR",
                BillingCycle = BillingCycle.Monthly,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            seed.Subscriptions.Add(new Subscription
            {
                Id = subscriptionId,
                CustomerId = customerId,
                PlanId = planId,
                Status = SubscriptionStatus.Active,
                StartDate = periodStart,
                NextBillingDate = periodStart,
                RowVersion = new byte[] { 0 },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await seed.SaveChangesAsync();
        }

        var successCount = 0;
        var tasks = Enumerable.Range(0, 8).Select(index => Task.Run(async () =>
        {
            await using var db = CreateContext();
            var seq = string.Format(CultureInfo.InvariantCulture, "{0:D6}", index + 1);
            var invoice = new Invoice(
                Guid.NewGuid(),
                subscriptionId,
                customerId,
                $"INV-202601-{seq}",
                periodStart,
                periodEnd,
                100000m,
                "IDR",
                InvoiceStatus.Pending,
                periodStart,
                periodStart.AddDays(14),
                key,
                DateTime.UtcNow);
            db.Invoices.Add(invoice);
            try
            {
                await db.SaveChangesAsync();
                Interlocked.Increment(ref successCount);
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }));

        await Task.WhenAll(tasks);

        await using var verify = CreateContext();
        var count = await verify.Invoices.CountAsync(i => i.IdempotencyKey == key);
        Assert.Equal(1, count);
        Assert.Equal(1, successCount);
    }
}
