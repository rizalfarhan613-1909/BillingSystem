using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingSystem.Infrastructure.Data;
using BillingSystem.Domain;
using BillingSystem.Api.DTOs;
using BillingSystem.Api.Validators;
using FluentValidation;

namespace BillingSystem.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SubscriptionsController : ControllerBase
{
    private readonly BillingDbContext _dbContext;
    private readonly IValidator<CreateSubscriptionRequest> _createValidator;

    public SubscriptionsController(
        BillingDbContext dbContext,
        IValidator<CreateSubscriptionRequest> createValidator)
    {
        _dbContext = dbContext;
        _createValidator = createValidator;
    }

    /// <summary>
    /// Membuat subscription baru.
    /// </summary>
    /// <param name="request">Data subscription baru.</param>
    /// <returns>Subscription yang baru dibuat.</returns>
    /// <response code="201">Subscription berhasil dibuat.</response>
    /// <response code="400">Request tidak valid (validasi gagal).</response>
    /// <response code="404">Customer atau Plan tidak ditemukan/tidak aktif.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubscriptionResponse>> CreateAsync(
        CreateSubscriptionRequest request,
        CancellationToken ct)
    {
        var validationResult = await _createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var customerExists = await _dbContext.Customers
            .AnyAsync(x => x.Id == request.CustomerId, ct);
        if (!customerExists)
        {
            return NotFound(new { message = "Customer tidak ditemukan." });
        }

        var planExists = await _dbContext.SubscriptionPlans
            .AnyAsync(x => x.Id == request.PlanId && x.IsActive, ct);
        if (!planExists)
        {
            return NotFound(new { message = "Plan tidak ditemukan atau tidak aktif." });
        }

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            PlanId = request.PlanId,
            Status = SubscriptionStatus.Active,
            StartDate = request.StartDate,
            NextBillingDate = request.NextBillingDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Subscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = subscription.Id },
            ToResponse(subscription));
    }

    /// <summary>
    /// Mendapatkan daftar semua subscription.
    /// </summary>
    /// <returns>Daftar subscription.</returns>
    /// <response code="200">Daftar subscription berhasil diambil.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<SubscriptionResponse>>> ListAsync(CancellationToken ct)
    {
        var subscriptions = await _dbContext.Subscriptions
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Plan)
            .OrderBy(x => x.CreatedAt)
            .Select(x => ToResponse(x))
            .ToListAsync(ct);

        return Ok(subscriptions);
    }

    /// <summary>
    /// Mendapatkan detail subscription berdasarkan ID.
    /// </summary>
    /// <param name="id">ID subscription.</param>
    /// <returns>Detail subscription.</returns>
    /// <response code="200">Detail subscription berhasil diambil.</response>
    /// <response code="404">Subscription tidak ditemukan.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubscriptionResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var subscription = await _dbContext.Subscriptions
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (subscription is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(subscription));
    }

    /// <summary>
    /// Menghentikan sementara subscription (pause).
    /// </summary>
    /// <param name="id">ID subscription.</param>
    /// <param name="request">Request body (kosong).</param>
    /// <returns>Subscription yang diperbarui.</returns>
    /// <response code="200">Subscription berhasil di-pause.</response>
    /// <response code="400">Subscription tidak berstatus Active.</response>
    /// <response code="404">Subscription tidak ditemukan.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpPatch("{id:guid}/pause")]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubscriptionResponse>> PauseAsync(
        Guid id,
        PauseSubscriptionRequest request,
        CancellationToken ct)
    {
        var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (subscription is null)
        {
            return NotFound();
        }

        if (subscription.Status != SubscriptionStatus.Active)
        {
            return BadRequest(new { message = "Hanya subscription Active yang dapat di-pause." });
        }

        subscription.Status = SubscriptionStatus.Paused;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return Ok(ToResponse(subscription));
    }

    /// <summary>
    /// Melanjutkan subscription yang di-pause (resume).
    /// </summary>
    /// <param name="id">ID subscription.</param>
    /// <param name="request">Request body (kosong).</param>
    /// <returns>Subscription yang diperbarui.</returns>
    /// <response code="200">Subscription berhasil di-resume.</response>
    /// <response code="400">Subscription tidak berstatus Paused.</response>
    /// <response code="404">Subscription tidak ditemukan.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpPatch("{id:guid}/resume")]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubscriptionResponse>> ResumeAsync(
        Guid id,
        ResumeSubscriptionRequest request,
        CancellationToken ct)
    {
        var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (subscription is null)
        {
            return NotFound();
        }

        if (subscription.Status != SubscriptionStatus.Paused)
        {
            return BadRequest(new { message = "Hanya subscription Paused yang dapat di-resume." });
        }

        subscription.Status = SubscriptionStatus.Active;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return Ok(ToResponse(subscription));
    }

    /// <summary>
    /// Membatalkan subscription.
    /// </summary>
    /// <param name="id">ID subscription.</param>
    /// <param name="request">Request body (kosong).</param>
    /// <returns>Subscription yang diperbarui.</returns>
    /// <response code="200">Subscription berhasil dibatalkan.</response>
    /// <response code="400">Subscription sudah dibatalkan.</response>
    /// <response code="404">Subscription tidak ditemukan.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubscriptionResponse>> CancelAsync(
        Guid id,
        CancelSubscriptionRequest request,
        CancellationToken ct)
    {
        var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (subscription is null)
        {
            return NotFound();
        }

        if (subscription.Status == SubscriptionStatus.Cancelled)
        {
            return BadRequest(new { message = "Subscription sudah dibatalkan." });
        }

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return Ok(ToResponse(subscription));
    }

    private static SubscriptionResponse ToResponse(Subscription subscription) =>
        new(
            subscription.Id,
            subscription.CustomerId,
            subscription.PlanId,
            (byte)subscription.Status,
            subscription.StartDate,
            subscription.NextBillingDate,
            subscription.EndDate,
            subscription.CancelledAt,
            subscription.CreatedAt);
}