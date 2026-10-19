using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingSystem.Domain;
using BillingSystem.Infrastructure.Data;

namespace BillingSystem.Api.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Produces("application/json")]
public class AuditLogsController : ControllerBase
{
    private readonly BillingDbContext _dbContext;

    public AuditLogsController(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Mendapatkan daftar audit log dengan filter dan pagination.
    /// </summary>
    /// <param name="entityName">Filter berdasarkan nama entitas (optional).</param>
    /// <param name="entityId">Filter berdasarkan ID entitas (optional).</param>
    /// <param name="action">Filter berdasarkan aksi (optional).</param>
    /// <param name="from">Filter dari tanggal (optional).</param>
    /// <param name="to">Filter sampai tanggal (optional).</param>
    /// <param name="page">Nomor halaman (default: 1).</param>
    /// <param name="pageSize">Ukuran halaman 1-100 (default: 50).</param>
    /// <returns>Daftar audit log yang difilter dan dipaginasi.</returns>
    /// <response code="200">Daftar audit log berhasil diambil.</response>
    /// <response code="400">Parameter query tidak valid.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<AuditLogResponse>>> ListAsync(
        [FromQuery] string? entityName,
        [FromQuery] string? entityId,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var query = _dbContext.AuditLogs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(a => a.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(a => a.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (from.HasValue)
        {
            query = query.Where(a => a.OccurredAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(a => a.OccurredAt <= to.Value);
        }

        query = query.OrderByDescending(a => a.OccurredAt).ThenBy(a => a.Id);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogResponse(
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

        return Ok(new PagedResult<AuditLogResponse>(items, page, pageSize, totalCount));
    }
}

public record AuditLogResponse(
    long Id,
    string EntityName,
    string EntityId,
    string Action,
    string? OldValue,
    string? NewValue,
    string PerformedBy,
    Guid CorrelationId,
    DateTime OccurredAt);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);