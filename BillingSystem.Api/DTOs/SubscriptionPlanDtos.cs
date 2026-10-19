namespace BillingSystem.Api.DTOs;

public record CreateSubscriptionPlanRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    byte BillingCycle
);

public record UpdateSubscriptionPlanRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    byte BillingCycle,
    bool IsActive
);

public record SubscriptionPlanResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    byte BillingCycle,
    bool IsActive,
    DateTime CreatedAt
);