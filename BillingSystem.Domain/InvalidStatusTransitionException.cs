namespace BillingSystem.Domain;

public sealed class InvalidStatusTransitionException : Exception
{
    public InvoiceStatus CurrentStatus { get; }
    public InvoiceStatus TargetStatus { get; }
    public Guid InvoiceId { get; }

    public InvalidStatusTransitionException(Guid invoiceId, InvoiceStatus currentStatus, InvoiceStatus targetStatus)
        : base($"Invalid status transition for invoice {invoiceId}: {currentStatus} → {targetStatus}.")
    {
        InvoiceId = invoiceId;
        CurrentStatus = currentStatus;
        TargetStatus = targetStatus;
    }
}