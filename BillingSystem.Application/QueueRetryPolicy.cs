using System;

namespace BillingSystem.Application;

public static class QueueRetryPolicy
{
    public static TimeSpan GetBackoff(long dequeueCount, Random rng)
    {
        var baseDelay = dequeueCount switch
        {
            <= 1 => TimeSpan.FromSeconds(30),
            2 => TimeSpan.FromMinutes(2),
            3 => TimeSpan.FromMinutes(10),
            _ => TimeSpan.FromMinutes(30)
        };
        var jitter = 1 + (rng.NextDouble() * 0.4 - 0.2); // ±20%
        return TimeSpan.FromTicks((long)(baseDelay.Ticks * jitter));
    }
}