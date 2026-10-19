using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingSystem.Application;
using BillingSystem.Domain;
using BillingSystem.Infrastructure.Data;
using System.Globalization;

namespace BillingSystem.Api.Controllers;

[ApiController]
[Route("api/v1/invoices")]
[Produces("application/json")]
public class InvoicesController : ControllerBase
{
    private readonly BillingDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public InvoicesController(BillingDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Mendapatkan daftar invoice dengan filter, pencarian, dan pagination.
    /// </summary>
    /// <param name="status">Filter berdasarkan status invoice (optional).</param>
    /// <param name="search">Pencarian berdasarkan nomor invoice, nama customer, atau email (optional).</param>
    /// <param name="sortBy">Field untuk sorting: InvoiceNumber, Amount, DueDate, IssueDate (default: IssueDate).</param>
    /// <param name="sortDir">Arah sorting: asc atau desc (default: desc).</param>
    /// <param name="page">Nomor halaman (default: 1).</param>
    /// <param name="pageSize">Ukuran halaman 1-100 (default: 10).</param>
    /// <returns>Daftar invoice yang difilter dan dipaginasi.</returns>
    /// <response code="200">Daftar invoice berhasil diambil.</response>
    /// <response code="400">Parameter query tidak valid.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ListAsync(
        [FromQuery] InvoiceStatus? status,
        [FromQuery] string? search,
        [FromQuery] string sortBy = "IssueDate",
        [FromQuery] string sortDir = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Subscription)
            .ThenInclude(s => s.Plan)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(i =>
                i.InvoiceNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.Customer.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.Customer.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        query = sortBy.ToLowerInvariant() switch
        {
            "invoicenumber" => sortDir.Equals("asc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(i => i.InvoiceNumber).ThenBy(i => i.Id)
                : query.OrderByDescending(i => i.InvoiceNumber).ThenBy(i => i.Id),
            "amount" => sortDir.Equals("asc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(i => i.Amount).ThenBy(i => i.Id)
                : query.OrderByDescending(i => i.Amount).ThenBy(i => i.Id),
            "duedate" => sortDir.Equals("asc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(i => i.DueDate).ThenBy(i => i.Id)
                : query.OrderByDescending(i => i.DueDate).ThenBy(i => i.Id),
            _ => sortDir.Equals("asc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(i => i.IssueDate).ThenBy(i => i.Id)
                : query.OrderByDescending(i => i.IssueDate).ThenBy(i => i.Id)
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.CustomerId,
                i.SubscriptionId,
                i.Status,
                i.Amount,
                i.Currency,
                i.BillingPeriodStart,
                i.BillingPeriodEnd,
                i.IssueDate,
                i.DueDate,
                i.PaidAt,
                i.FailureReason,
                i.CreatedAt,
                i.Customer.FullName,
                i.Customer.Email,
                i.Subscription.Plan.Name))
            .ToListAsync(ct);

        return Ok(new { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount });
    }

    /// <summary>
    /// Mendapatkan detail invoice berdasarkan ID.
    /// </summary>
    /// <param name="id">ID invoice.</param>
    /// <returns>Detail invoice.</returns>
    /// <response code="200">Detail invoice berhasil diambil.</response>
    /// <response code="404">Invoice tidak ditemukan.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InvoiceDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var invoice = await _dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Subscription)
            .ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice is null)
        {
            return NotFound();
        }

        return Ok(new InvoiceDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerId,
            invoice.SubscriptionId,
            invoice.Status,
            invoice.Amount,
            invoice.Currency,
            invoice.BillingPeriodStart,
            invoice.BillingPeriodEnd,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.PaidAt,
            invoice.FailureReason,
            invoice.CreatedAt,
            invoice.Customer.FullName,
            invoice.Customer.Email,
            invoice.Subscription.Plan.Name));
    }

    /// <summary>
    /// Mengubah status invoice.
    /// </summary>
    /// <param name="id">ID invoice.</param>
    /// <param name="request">Request berisi status baru dan alasan (optional).</param>
    /// <returns>Invoice dengan status yang diperbarui.</returns>
    /// <response code="200">Status invoice berhasil diubah.</response>
    /// <response code="400">Request body tidak valid.</response>
    /// <response code="404">Invoice tidak ditemukan.</response>
    /// <response code="409">Transisi status tidak valid (misal: dari Paid ke Draft).</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InvoiceDto>> ChangeStatusAsync(
        Guid id,
        [FromBody] ChangeInvoiceStatusRequest request,
        CancellationToken ct)
    {
        var invoice = await _dbContext.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Subscription)
            .ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice is null)
        {
            return NotFound();
        }

        try
        {
            invoice.TransitionTo(request.Status, _timeProvider.GetUtcNow(), request.Reason);
        }
        catch (InvalidStatusTransitionException ex)
        {
            return Conflict(new
            {
                type = "https://billing.local/problems/invalid-status-transition",
                title = "Transisi status tidak valid",
                status = 409,
                detail = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }

        var auditLog = new AuditLog
        {
            EntityName = "Invoice",
            EntityId = invoice.Id.ToString(),
            Action = "InvoiceStatusChanged",
            OldValue = null,
            NewValue = $"Status={request.Status}",
            PerformedBy = "System",
            CorrelationId = Guid.NewGuid(),
            OccurredAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(new InvoiceDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerId,
            invoice.SubscriptionId,
            invoice.Status,
            invoice.Amount,
            invoice.Currency,
            invoice.BillingPeriodStart,
            invoice.BillingPeriodEnd,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.PaidAt,
            invoice.FailureReason,
            invoice.CreatedAt,
            invoice.Customer.FullName,
            invoice.Customer.Email,
            invoice.Subscription.Plan.Name));
    }

public record ChangeInvoiceStatusRequest(InvoiceStatus Status, string? Reason);

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    Guid SubscriptionId,
    InvoiceStatus Status,
    decimal Amount,
    string Currency,
    DateOnly BillingPeriodStart,
    DateOnly BillingPeriodEnd,
    DateOnly IssueDate,
    DateOnly DueDate,
    DateTime? PaidAt,
    string? FailureReason,
    DateTime CreatedAt,
    string CustomerFullName,
    string CustomerEmail,
    string PlanName);
}