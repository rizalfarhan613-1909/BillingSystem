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
public class SubscriptionPlansController : ControllerBase
{
    private readonly BillingDbContext _dbContext;
    private readonly IValidator<CreateSubscriptionPlanRequest> _createValidator;
    private readonly IValidator<UpdateSubscriptionPlanRequest> _updateValidator;

    public SubscriptionPlansController(
        BillingDbContext dbContext,
        IValidator<CreateSubscriptionPlanRequest> createValidator,
        IValidator<UpdateSubscriptionPlanRequest> updateValidator)
    {
        _dbContext = dbContext;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Membuat subscription plan baru.
    /// </summary>
    /// <param name="request">Data plan baru.</param>
    /// <returns>Plan yang baru dibuat.</returns>
    /// <response code="201">Plan berhasil dibuat.</response>
    /// <response code="400">Request tidak valid (validasi gagal).</response>
    /// <response code="409">Nama plan sudah terdaftar.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubscriptionPlanResponse>> CreateAsync(
        CreateSubscriptionPlanRequest request,
        CancellationToken ct)
    {
        var validationResult = await _createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var planName = request.Name.Trim();

        var exists = await _dbContext.SubscriptionPlans
            .AnyAsync(x => x.Name == planName, ct);
        if (exists)
        {
            return Conflict(new { message = "Nama plan sudah terdaftar." });
        }

        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = planName,
            Description = request.Description?.Trim(),
            Price = request.Price,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            BillingCycle = (BillingCycle)request.BillingCycle,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.SubscriptionPlans.Add(plan);
        await _dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = plan.Id },
            ToResponse(plan));
    }

    /// <summary>
    /// Mendapatkan daftar semua subscription plan.
    /// </summary>
    /// <returns>Daftar plan.</returns>
    /// <response code="200">Daftar plan berhasil diambil.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<SubscriptionPlanResponse>>> ListAsync(CancellationToken ct)
    {
        var plans = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => ToResponse(x))
            .ToListAsync(ct);

        return Ok(plans);
    }

    /// <summary>
    /// Mendapatkan detail subscription plan berdasarkan ID.
    /// </summary>
    /// <param name="id">ID plan.</param>
    /// <returns>Detail plan.</returns>
    /// <response code="200">Detail plan berhasil diambil.</response>
    /// <response code="404">Plan tidak ditemukan.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubscriptionPlanResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var plan = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (plan is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(plan));
    }

    /// <summary>
    /// Memperbarui subscription plan.
    /// </summary>
    /// <param name="id">ID plan.</param>
    /// <param name="request">Data plan yang diperbarui.</param>
    /// <returns>Plan yang diperbarui.</returns>
    /// <response code="200">Plan berhasil diperbarui.</response>
    /// <response code="400">Request tidak valid (validasi gagal).</response>
    /// <response code="404">Plan tidak ditemukan.</response>
    /// <response code="409">Nama plan sudah digunakan oleh plan lain.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubscriptionPlanResponse>> UpdateAsync(
        Guid id,
        UpdateSubscriptionPlanRequest request,
        CancellationToken ct)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var plan = await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (plan is null)
        {
            return NotFound();
        }

        var planName = request.Name.Trim();

        var nameExists = await _dbContext.SubscriptionPlans
            .AnyAsync(x => x.Name == planName && x.Id != id, ct);
        if (nameExists)
        {
            return Conflict(new { message = "Nama plan sudah digunakan oleh plan lain." });
        }

        plan.Name = planName;
        plan.Description = request.Description?.Trim();
        plan.Price = request.Price;
        plan.Currency = request.Currency.Trim().ToUpperInvariant();
        plan.BillingCycle = (BillingCycle)request.BillingCycle;
        plan.IsActive = request.IsActive;
        plan.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return Ok(ToResponse(plan));
    }

    /// <summary>
    /// Menonaktifkan subscription plan (soft delete).
    /// </summary>
    /// <param name="id">ID plan.</param>
    /// <response code="204">Plan berhasil dinonaktifkan.</response>
    /// <response code="404">Plan tidak ditemukan.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeactivateAsync(Guid id, CancellationToken ct)
    {
        var plan = await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (plan is null)
        {
            return NotFound();
        }

        plan.IsActive = false;
        plan.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return NoContent();
    }

    private static SubscriptionPlanResponse ToResponse(SubscriptionPlan plan) =>
        new(
            plan.Id,
            plan.Name,
            plan.Description,
            plan.Price,
            plan.Currency,
            (byte)plan.BillingCycle,
            plan.IsActive,
            plan.CreatedAt);
}