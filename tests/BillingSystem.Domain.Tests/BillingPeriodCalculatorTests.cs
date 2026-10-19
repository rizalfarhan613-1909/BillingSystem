using BillingSystem.Application;
using BillingSystem.Domain;

namespace BillingSystem.Domain.Tests;

public sealed class BillingPeriodCalculatorTests
{
    [Fact]
    public void GetPeriodStart_Monthly_AddsMonths()
    {
        var anchor = new DateOnly(2026, 1, 15);

        Assert.Equal(new DateOnly(2026, 1, 15), BillingPeriodCalculator.GetPeriodStart(anchor, BillingCycle.Monthly, 0));
        Assert.Equal(new DateOnly(2026, 2, 15), BillingPeriodCalculator.GetPeriodStart(anchor, BillingCycle.Monthly, 1));
    }

    [Fact]
    public void GetPeriodStart_Quarterly_AddsThreeMonths()
    {
        var anchor = new DateOnly(2026, 1, 1);

        Assert.Equal(new DateOnly(2026, 4, 1), BillingPeriodCalculator.GetPeriodStart(anchor, BillingCycle.Quarterly, 1));
    }

    [Fact]
    public void GetPeriodStart_Yearly_AddsTwelveMonths()
    {
        var anchor = new DateOnly(2026, 1, 1);

        Assert.Equal(new DateOnly(2027, 1, 1), BillingPeriodCalculator.GetPeriodStart(anchor, BillingCycle.Yearly, 1));
    }

    [Fact]
    public void GetPeriodEnd_IsDayBeforeNextStart()
    {
        var anchor = new DateOnly(2026, 1, 15);

        Assert.Equal(new DateOnly(2026, 2, 14), BillingPeriodCalculator.GetPeriodEnd(anchor, BillingCycle.Monthly, 0));
    }

    [Theory]
    [InlineData(BillingCycle.Monthly, 1)]
    [InlineData(BillingCycle.Quarterly, 3)]
    [InlineData(BillingCycle.Yearly, 12)]
    public void MonthsOf_ReturnsExpected(BillingCycle cycle, int expected)
    {
        Assert.Equal(expected, BillingPeriodCalculator.MonthsOf(cycle));
    }
}
