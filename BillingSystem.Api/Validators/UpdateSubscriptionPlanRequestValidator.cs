using FluentValidation;
using BillingSystem.Api.DTOs;

namespace BillingSystem.Api.Validators;

public sealed class UpdateSubscriptionPlanRequestValidator : AbstractValidator<UpdateSubscriptionPlanRequest>
{
    public UpdateSubscriptionPlanRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name wajib diisi.")
            .MaximumLength(100).WithMessage("Name maksimal 100 karakter.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description maksimal 500 karakter.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price harus lebih besar dari nol.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency wajib diisi.")
            .Length(3).WithMessage("Currency harus 3 karakter (kode ISO 4217).");

        RuleFor(x => x.BillingCycle)
            .IsInEnum().WithMessage("BillingCycle tidak valid.");
    }
}