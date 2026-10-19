namespace BillingSystem.Application;

public record ScanRequestMessage(
    Guid RunId,
    DateOnly? AsOfDate,
    Guid CorrelationId
);

public record InvoiceJobMessage(
    Guid SubscriptionId,
    DateOnly BillingPeriodStart,
    DateOnly BillingPeriodEnd,
    string IdempotencyKey,
    Guid CorrelationId
);