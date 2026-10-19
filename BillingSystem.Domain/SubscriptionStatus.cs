namespace BillingSystem.Domain;

public enum SubscriptionStatus : byte
{
    Active = 1,
    Paused = 2,
    Cancelled = 3,
    Expired = 4
}