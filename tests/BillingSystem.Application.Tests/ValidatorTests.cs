using BillingSystem.Api.DTOs;
using BillingSystem.Api.Validators;

namespace BillingSystem.Application.Tests;

public sealed class ValidatorTests
{
    [Fact]
    public void CreateCustomerValidator_ValidRequest_Passes()
    {
        var validator = new CreateCustomerRequestValidator();
        var request = new CreateCustomerRequest("John Doe", "john@example.com", null, null);

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateCustomerValidator_InvalidEmail_Fails()
    {
        var validator = new CreateCustomerRequestValidator();
        var request = new CreateCustomerRequest("John Doe", "not-an-email", null, null);

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateSubscriptionValidator_NextBillingBeforeStart_Fails()
    {
        var validator = new CreateSubscriptionRequestValidator();
        var request = new CreateSubscriptionRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 1, 1));

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
