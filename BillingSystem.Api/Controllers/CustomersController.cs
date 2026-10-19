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
public class CustomersController : ControllerBase
{
    private readonly BillingDbContext _dbContext;
    private readonly IValidator<CreateCustomerRequest> _createValidator;
    private readonly IValidator<UpdateCustomerRequest> _updateValidator;

    public CustomersController(
        BillingDbContext dbContext,
        IValidator<CreateCustomerRequest> createValidator,
        IValidator<UpdateCustomerRequest> updateValidator)
    {
        _dbContext = dbContext;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Membuat customer baru.
    /// </summary>
    /// <param name="request">Data customer baru.</param>
    /// <returns>Customer yang baru dibuat.</returns>
    /// <response code="201">Customer berhasil dibuat.</response>
    /// <response code="400">Request tidak valid (validasi gagal).</response>
    /// <response code="409">Email sudah terdaftar.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CustomerResponse>> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken ct)
    {
        var validationResult = await _createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var emailNormalized = request.Email.Trim().ToLowerInvariant();

        var exists = await _dbContext.Customers
            .AnyAsync(x => x.Email == emailNormalized, ct);
        if (exists)
        {
            return Conflict(new { message = "Email sudah terdaftar." });
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = emailNormalized,
            CompanyName = request.CompanyName?.Trim(),
            BillingAddress = request.BillingAddress?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = customer.Id },
            ToResponse(customer));
    }

    /// <summary>
    /// Mendapatkan daftar semua customer.
    /// </summary>
    /// <returns>Daftar customer.</returns>
    /// <response code="200">Daftar customer berhasil diambil.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CustomerResponse>>> ListAsync(CancellationToken ct)
    {
        var customers = await _dbContext.Customers
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .Select(x => ToResponse(x))
            .ToListAsync(ct);

        return Ok(customers);
    }

    /// <summary>
    /// Mendapatkan detail customer berdasarkan ID.
    /// </summary>
    /// <param name="id">ID customer.</param>
    /// <returns>Detail customer.</returns>
    /// <response code="200">Detail customer berhasil diambil.</response>
    /// <response code="404">Customer tidak ditemukan.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CustomerResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (customer is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(customer));
    }

    /// <summary>
    /// Memperbarui data customer.
    /// </summary>
    /// <param name="id">ID customer.</param>
    /// <param name="request">Data customer yang diperbarui.</param>
    /// <returns>Customer yang diperbarui.</returns>
    /// <response code="200">Customer berhasil diperbarui.</response>
    /// <response code="400">Request tidak valid (validasi gagal).</response>
    /// <response code="404">Customer tidak ditemukan.</response>
    /// <response code="409">Email sudah digunakan oleh customer lain.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CustomerResponse>> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken ct)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var customer = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (customer is null)
        {
            return NotFound();
        }

        var emailNormalized = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _dbContext.Customers
            .AnyAsync(x => x.Email == emailNormalized && x.Id != id, ct);
        if (emailExists)
        {
            return Conflict(new { message = "Email sudah digunakan oleh customer lain." });
        }

        customer.FullName = request.FullName.Trim();
        customer.Email = emailNormalized;
        customer.CompanyName = request.CompanyName?.Trim();
        customer.BillingAddress = request.BillingAddress?.Trim();
        customer.IsActive = request.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return Ok(ToResponse(customer));
    }

    /// <summary>
    /// Menonaktifkan customer (soft delete).
    /// </summary>
    /// <param name="id">ID customer.</param>
    /// <response code="204">Customer berhasil dinonaktifkan.</response>
    /// <response code="404">Customer tidak ditemukan.</response>
    /// <response code="500">Terjadi kesalahan internal server.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (customer is null)
        {
            return NotFound();
        }

        customer.IsActive = false;
        customer.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return NoContent();
    }

    private static CustomerResponse ToResponse(Customer customer) =>
        new(
            customer.Id,
            customer.FullName,
            customer.Email,
            customer.CompanyName,
            customer.BillingAddress,
            customer.IsActive,
            customer.CreatedAt);
}