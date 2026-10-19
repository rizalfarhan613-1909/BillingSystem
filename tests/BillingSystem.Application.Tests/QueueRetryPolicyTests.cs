using BillingSystem.Application;

namespace BillingSystem.Application.Tests;

public sealed class QueueRetryPolicyTests
{
    [Fact]
    public void GetBackoff_FirstAttempt_AroundThirtySeconds()
    {
        var backoff = QueueRetryPolicy.GetBackoff(1, new Random(42));

        Assert.InRange(backoff.TotalSeconds, 24, 36);
    }

    [Fact]
    public void GetBackoff_SecondAttempt_AroundTwoMinutes()
    {
        var backoff = QueueRetryPolicy.GetBackoff(2, new Random(42));

        Assert.InRange(backoff.TotalSeconds, 96, 144);
    }

    [Fact]
    public void GetBackoff_ThirdAttempt_AroundTenMinutes()
    {
        var backoff = QueueRetryPolicy.GetBackoff(3, new Random(42));

        Assert.InRange(backoff.TotalSeconds, 480, 720);
    }

    [Fact]
    public void GetBackoff_FourthAttempt_AroundThirtyMinutes()
    {
        var backoff = QueueRetryPolicy.GetBackoff(4, new Random(42));

        Assert.InRange(backoff.TotalSeconds, 1440, 2160);
    }

    [Fact]
    public void GetBackoff_BeyondFourth_CapsAtThirtyMinutes()
    {
        var backoff = QueueRetryPolicy.GetBackoff(10, new Random(42));

        Assert.InRange(backoff.TotalSeconds, 1440, 2160);
    }
}
