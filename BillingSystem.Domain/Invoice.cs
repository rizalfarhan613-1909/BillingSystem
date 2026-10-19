namespace BillingSystem.Domain;

public sealed class Invoice
{
    public Guid Id { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public DateOnly BillingPeriodStart { get; private set; }
    public DateOnly BillingPeriodEnd { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "IDR";
    public InvoiceStatus Status { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public string? FailureReason { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Subscription Subscription { get; set; } = null!;
    public Customer Customer { get; set; } = null!;

    private Invoice() { }

    public Invoice(
        Guid id,
        Guid subscriptionId,
        Guid customerId,
        string invoiceNumber,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        decimal amount,
        string currency,
        InvoiceStatus status,
        DateOnly issueDate,
        DateOnly dueDate,
        string idempotencyKey,
        DateTime createdAt)
    {
        Id = id;
        SubscriptionId = subscriptionId;
        CustomerId = customerId;
        InvoiceNumber = invoiceNumber;
        BillingPeriodStart = billingPeriodStart;
        BillingPeriodEnd = billingPeriodEnd;
        Amount = amount;
        Currency = currency;
        Status = status;
        IssueDate = issueDate;
        DueDate = dueDate;
        IdempotencyKey = idempotencyKey;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public void TransitionTo(InvoiceStatus targetStatus, DateTimeOffset now, string? reason = null)
    {
        // BR-51: Paid is terminal and immutable
        if (Status == InvoiceStatus.Paid)
        {
            throw new InvalidStatusTransitionException(Id, Status, targetStatus);
        }

        // Validate transition matrix (BR-50)
        bool isValid = (Status, targetStatus) switch
        {
            (InvoiceStatus.Pending, InvoiceStatus.Paid) => true,
            (InvoiceStatus.Pending, InvoiceStatus.Overdue) => true,
            (InvoiceStatus.Pending, InvoiceStatus.Failed) => true,
            (InvoiceStatus.Overdue, InvoiceStatus.Paid) => true,
            (InvoiceStatus.Overdue, InvoiceStatus.Failed) => true,
            (InvoiceStatus.Failed, InvoiceStatus.Pending) => true,
            _ => false
        };

        if (!isValid)
        {
            throw new InvalidStatusTransitionException(Id, Status, targetStatus);
        }

        // BR-52: Failed requires FailureReason
        if (targetStatus == InvoiceStatus.Failed && string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("FailureReason is required when transitioning to Failed.", nameof(reason));
        }

        Status = targetStatus;
        UpdatedAt = now.UtcDateTime;

        if (targetStatus == InvoiceStatus.Paid)
        {
            PaidAt = now.UtcDateTime;
        }

        if (targetStatus == InvoiceStatus.Failed)
        {
            FailureReason = reason;
        }
    }
}