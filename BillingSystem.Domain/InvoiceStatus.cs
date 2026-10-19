namespace BillingSystem.Domain;

public enum InvoiceStatus : byte
{
    Pending = 1,
    Paid = 2,
    Overdue = 3,
    Failed = 4
}