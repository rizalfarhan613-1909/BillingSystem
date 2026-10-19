namespace BillingSystem.Api.DTOs;

public record CreateSubscriptionRequest(
    Guid CustomerId,
    Guid PlanId,
    DateOnly StartDate,
    DateOnly NextBillingDate
);

public record SubscriptionResponse(
    Guid Id,
    Guid CustomerId,
    Guid PlanId,
    byte Status,
    DateOnly StartDate,
    DateOnly NextBillingDate,
    DateOnly? EndDate,
    DateTime? CancelledAt,
    DateTime CreatedAt
);

public record PauseSubscriptionRequest(
    string? Reason
);

public record ResumeSubscriptionRequest(
    string? Reason
);

public record CancelSubscriptionRequest(
    string? Reason
);