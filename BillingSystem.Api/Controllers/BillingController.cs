using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingSystem.Infrastructure.Data;
using BillingSystem.Application;
using BillingSystem.Domain;
using BillingSystem.Api.DTOs;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace BillingSystem.Api.Controllers;

[ApiController]
[Route("api/v1/billing")]
[Produces("application/json")]
public class BillingController : ControllerBase
{
    private readonly BillingDbContext _dbContext;
    private readonly IQueuePublisher _queuePublisher;
    private readonly QueueOptions _queueOptions;
    private readonly BillingOptions _billingOptions;

    public BillingController(
        BillingDbContext dbContext,
        IQueuePublisher queuePublisher,
        IOptions<QueueOptions> queueOptions,
        IOptions<BillingOptions> billingOptions)
    {
        _dbContext = dbContext;
        _queuePublisher = queuePublisher;
        _queueOptions = queueOptions.Value;
        _billingOptions = billingOptions.Value;
    }

    /// <summary>
    /// Memicu proses penagihan (billing run) secara manual.
    /// </summary>
    /// <param name="request">Request berisi tanggal simulasi (optional, hanya diizinkan jika AllowSimulatedDate=true).</param>
    /// <returns>Informasi billing run yang diterima.</returns>
    /// <response code="202">Billing run diterima dan dimasukkan ke antrean.</response>
    /// <response code="400">Simulated date tidak diizinkan atau request tidak valid.</response>
    /// <response code="503">Gagal memasukkan ke antrean (queue).</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpPost("trigger")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TriggerAsync(
        [FromBody] TriggerBillingRequest request,
        CancellationToken ct)
    {
        var correlationId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var asOfDate = request.AsOfDate;

        if (asOfDate.HasValue && !_billingOptions.AllowSimulatedDate)
        {
            return BadRequest(new { message = "Simulated date is not allowed." });
        }

        var asOfDateString = asOfDate.HasValue ? asOfDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "null";
        var auditLog = new AuditLog
        {
            EntityName = "BillingRun",
            EntityId = runId.ToString(),
            Action = "BillingRunRequested",
            OldValue = null,
            NewValue = $"{{\"asOfDate\":\"{asOfDateString}\"}}",
            PerformedBy = "System",
            CorrelationId = correlationId,
            OccurredAt = DateTime.UtcNow
        };

        _dbContext.AuditLogs.Add(auditLog);

        try
        {
            await _dbContext.SaveChangesAsync(ct);

            var message = new ScanRequestMessage(runId, asOfDate, correlationId);
            await _queuePublisher.PublishAsync(_queueOptions.ScanRequests, message, ct);

            return Accepted(new
            {
                runId,
                status = "Accepted",
                requestedAt = DateTime.UtcNow
            });
        }
        catch (Exception)
        {
            var failedAudit = new AuditLog
            {
                EntityName = "BillingRun",
                EntityId = runId.ToString(),
                Action = "BillingRunEnqueueFailed",
                OldValue = null,
                NewValue = "Enqueue to queue failed",
                PerformedBy = "System",
                CorrelationId = correlationId,
                OccurredAt = DateTime.UtcNow
            };
            _dbContext.AuditLogs.Add(failedAudit);
            await _dbContext.SaveChangesAsync(ct);

            return StatusCode(503, new { message = "Billing trigger enqueue failed." });
        }
    }
}

public record TriggerBillingRequest(DateOnly? AsOfDate);