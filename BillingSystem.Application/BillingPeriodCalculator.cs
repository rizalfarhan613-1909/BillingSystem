using BillingSystem.Domain;

namespace BillingSystem.Application;

public static class BillingPeriodCalculator
{
    public static DateOnly GetPeriodStart(DateOnly anchor, BillingCycle cycle, int periodIndex)
        => anchor.AddMonths(MonthsOf(cycle) * periodIndex);

    public static DateOnly GetPeriodEnd(DateOnly anchor, BillingCycle cycle, int periodIndex)
        => GetPeriodStart(anchor, cycle, periodIndex + 1).AddDays(-1);

    public static int MonthsOf(BillingCycle cycle) => cycle switch
    {
        BillingCycle.Monthly => 1,
        BillingCycle.Quarterly => 3,
        BillingCycle.Yearly => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(cycle))
    };
}