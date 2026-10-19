using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingSystem.Domain;
using BillingSystem.Infrastructure.Data;
using BillingSystem.Api.DTOs;

namespace BillingSystem.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly BillingDbContext _dbContext;

    public DashboardController(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Mendapatkan ringkasan performa penagihan.
    /// </summary>
    /// <returns>Rekap: Total Active Subscriptions, Total Pendapatan Bulan Ini (Paid), Total Tagihan Tertunggak (Overdue).</returns>
    /// <response code="200">Ringkasan dashboard berhasil diambil.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);

        // Total Active Subscriptions
        var totalActiveSubscriptions = await _dbContext.Subscriptions
            .AsNoTracking()
            .CountAsync(s => s.Status == SubscriptionStatus.Active, ct);

        // Total Revenue This Month (Paid invoices issued this month)
        var totalRevenueThisMonth = await _dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Paid &&
                        i.IssueDate >= DateOnly.FromDateTime(startOfMonth) &&
                        i.IssueDate <= DateOnly.FromDateTime(endOfMonth))
            .SumAsync(i => i.Amount, ct);

        // Total Overdue Invoices
        var totalOverdueInvoices = await _dbContext.Invoices
            .AsNoTracking()
            .CountAsync(i => i.Status == InvoiceStatus.Overdue, ct);

        var summary = new DashboardSummaryDto(
            totalActiveSubscriptions,
            totalRevenueThisMonth,
            totalOverdueInvoices);

        return Ok(summary);
    }

    /// <summary>
    /// Mendapatkan 10 riwayat audit log terbaru dan 10 invoice terakhir yang diproses.
    /// </summary>
    /// <returns>Transaksi terbaru (audit logs + invoices).</returns>
    /// <response code="200">Data transaksi terbaru berhasil diambil.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpGet("recent-transactions")]
    [ProducesResponseType(typeof(DashboardRecentTransactionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardRecentTransactionsDto>> GetRecentTransactionsAsync(CancellationToken ct)
    {
        // 10 Audit Log terbaru
        var recentAuditLogs = await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.OccurredAt)
            .Take(10)
            .Select(a => new RecentAuditLogDto(
                a.Id,
                a.EntityName,
                a.EntityId,
                a.Action,
                a.OldValue,
                a.NewValue,
                a.PerformedBy,
                a.CorrelationId,
                a.OccurredAt))
            .ToListAsync(ct);

        // 10 Invoice terakhir yang diproses (berdasarkan CreatedAt)
        var recentInvoices = await _dbContext.Invoices
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .Take(10)
            .Select(i => new RecentInvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.CustomerId,
                i.SubscriptionId,
                i.Status,
                i.Amount,
                i.Currency,
                i.IssueDate,
                i.DueDate,
                i.CreatedAt))
            .ToListAsync(ct);

        var result = new DashboardRecentTransactionsDto(recentAuditLogs, recentInvoices);
        return Ok(result);
    }
}