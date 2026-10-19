using FluentValidation;
using BillingSystem.Api.DTOs;

namespace BillingSystem.Api.Validators;

public sealed class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequest>
{
    public CreateSubscriptionRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId wajib diisi.");

        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId wajib diisi.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("StartDate wajib diisi.");

        RuleFor(x => x.NextBillingDate)
            .NotEmpty().WithMessage("NextBillingDate wajib diisi.")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("NextBillingDate tidak boleh lebih awal dari StartDate.");
    }
}