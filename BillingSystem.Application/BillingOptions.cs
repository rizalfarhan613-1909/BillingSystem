namespace BillingSystem.Application;

public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    public string BusinessTimeZone { get; set; } = "Asia/Jakarta";
    public int ScanIntervalMinutes { get; set; } = 60;
    public int PaymentTermDays { get; set; } = 14;
    public int OverdueGraceDays { get; set; } = 30;
    public bool AllowSimulatedDate { get; set; } = true;
}