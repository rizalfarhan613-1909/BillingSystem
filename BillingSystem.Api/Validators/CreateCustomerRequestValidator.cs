using FluentValidation;
using BillingSystem.Api.DTOs;

namespace BillingSystem.Api.Validators;

public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName wajib diisi.")
            .MaximumLength(200).WithMessage("FullName maksimal 200 karakter.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .EmailAddress().WithMessage("Format email tidak valid.")
            .MaximumLength(256).WithMessage("Email maksimal 256 karakter.");

        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("CompanyName maksimal 200 karakter.");

        RuleFor(x => x.BillingAddress)
            .MaximumLength(500).WithMessage("BillingAddress maksimal 500 karakter.");
    }
}