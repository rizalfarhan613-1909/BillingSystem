using BillingSystem.Application;
using BillingSystem.Domain;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BillingSystem.Worker;

#pragma warning disable CA1848
public sealed class OverdueSweeperService : BackgroundService
{
    private const int MaxBatchSize = 500; // BR-46

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OverdueSweeperService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly int _overdueGraceDays;
    private readonly int _scanIntervalMinutes;

    public OverdueSweeperService(
        IServiceScopeFactory scopeFactory,
        ILogger<OverdueSweeperService> logger,
        TimeProvider timeProvider,
        IOptions<BillingOptions> billingOptions)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timeProvider = timeProvider;
        _overdueGraceDays = billingOptions.Value.OverdueGraceDays;
        _scanIntervalMinutes = billingOptions.Value.ScanIntervalMinutes;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_scanIntervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Overdue sweeper failed.");
            }
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow();
        var businessDate = DateOnly.FromDateTime(now.UtcDateTime);

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var strategy = dbContext.Database.CreateExecutionStrategy(); // Rules E8

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                // 1. Pending → Overdue (DueDate < business date)
                var pendingOverdue = await dbContext.Invoices
                    .Where(i => i.Status == InvoiceStatus.Pending && i.DueDate < businessDate)
                    .OrderBy(i => i.Id)
                    .Take(MaxBatchSize)
                    .ToListAsync(ct);

                foreach (var invoice in pendingOverdue)
                {
                    invoice.TransitionTo(InvoiceStatus.Overdue, now, null);
                    dbContext.AuditLogs.Add(new AuditLog
                    {
                        EntityName = "Invoice",
                        EntityId = invoice.Id.ToString(),
                        Action = "InvoiceStatusChanged",
                        OldValue = null,
                        NewValue = $"Status=Overdue",
                        PerformedBy = "System",
                        CorrelationId = Guid.NewGuid(),
                        OccurredAt = now.UtcDateTime
                    });
                }

                // 2. Overdue → Failed (past grace period)
                var graceCutoff = businessDate.AddDays(-_overdueGraceDays);
                var overdueFailed = await dbContext.Invoices
                    .Where(i => i.Status == InvoiceStatus.Overdue && i.DueDate < graceCutoff)
                    .OrderBy(i => i.Id)
                    .Take(MaxBatchSize)
                    .ToListAsync(ct);

                foreach (var invoice in overdueFailed)
                {
                    invoice.TransitionTo(InvoiceStatus.Failed, now, "Exceeded overdue grace period");
                    dbContext.AuditLogs.Add(new AuditLog
                    {
                        EntityName = "Invoice",
                        EntityId = invoice.Id.ToString(),
                        Action = "InvoiceStatusChanged",
                        OldValue = null,
                        NewValue = $"Status=Failed",
                        PerformedBy = "System",
                        CorrelationId = Guid.NewGuid(),
                        OccurredAt = now.UtcDateTime
                    });
                }

                await dbContext.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation(
                    "Sweep completed. Pending→Overdue: {PendingCount}, Overdue→Failed: {FailedCount}.",
                    pendingOverdue.Count,
                    overdueFailed.Count);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

#pragma warning restore CA1848
}