namespace BillingSystem.Domain;

public sealed class AuditLog
{
    public long Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; }
}