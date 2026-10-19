namespace BillingSystem.Application;

public static class IdempotencyKeyFactory
{
    public static string Create(Guid subscriptionId, DateOnly billingPeriodStart)
        => $"INV:{subscriptionId}:{billingPeriodStart:yyyyMMdd}";
}