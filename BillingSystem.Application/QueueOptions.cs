namespace BillingSystem.Application;

public sealed class QueueOptions
{
    public const string SectionName = "Queues";

    public string ScanRequests { get; set; } = "billing-scan-requests";
    public string InvoiceJobs { get; set; } = "billing-invoice-jobs";
    public string InvoiceJobsPoison { get; set; } = "billing-invoice-jobs-poison";
    public int MaxDequeueCount { get; set; } = 5;
    public int MaxConcurrency { get; set; } = 8;
    public int VisibilityTimeoutSeconds { get; set; } = 120;
    public int MessageTtlDays { get; set; } = 7;
}