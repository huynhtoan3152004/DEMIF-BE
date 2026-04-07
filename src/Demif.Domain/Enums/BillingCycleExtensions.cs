namespace Demif.Domain.Enums;

/// <summary>
/// Helpers for mapping billing cycles to fixed durations.
/// </summary>
public static class BillingCycleExtensions
{
    public static bool IsSupportedPremiumCycle(this BillingCycle cycle)
        => cycle is BillingCycle.Weekly or BillingCycle.Monthly or BillingCycle.Yearly;

    public static int? GetDurationDays(this BillingCycle cycle)
    {
        return cycle switch
        {
            BillingCycle.Weekly => 7,
            BillingCycle.Monthly => 30,
            BillingCycle.Yearly => 365,
            BillingCycle.Lifetime => null,
            _ => throw new ArgumentOutOfRangeException(nameof(cycle), cycle, "Unsupported billing cycle.")
        };
    }
}