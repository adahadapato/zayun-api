namespace Zayun.Application.Common.Time;

/// <summary>
/// Today's date in the UK (London time, including summer time), so the quote of the day changes at midnight in London,
/// whatever time zone the server is in.
/// </summary>
/// <remarks>Uses <see cref="TimeProvider"/>, so tests can freeze time.</remarks>
public static class UkCalendar
{
    /// <summary>London's time zone: "Europe/London" (Linux, and Windows with ICU), or "GMT Standard Time" (older Windows).</summary>
    private static readonly TimeZoneInfo London = FindLondon();

    /// <summary>Today's date in the UK.</summary>
    /// <param name="clock">The clock.</param>
    /// <returns>The date.</returns>
    public static DateOnly Today(TimeProvider clock) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), London).DateTime);

    /// <summary>How long until the next midnight in the UK.</summary>
    /// <param name="clock">The clock.</param>
    /// <returns>The time left (always more than zero).</returns>
    public static TimeSpan UntilMidnight(TimeProvider clock)
    {
        var nowUtc = clock.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, London);

        // Midnight is never skipped or repeated in the UK (the clocks change at 1 am / 2 am), so this is safe.
        var nextMidnight = DateTime.SpecifyKind(localNow.Date.AddDays(1), DateTimeKind.Unspecified);
        var nextMidnightUtc = TimeZoneInfo.ConvertTimeToUtc(nextMidnight, London);

        var left = nextMidnightUtc - nowUtc.UtcDateTime;
        return left > TimeSpan.Zero ? left : TimeSpan.FromSeconds(1);
    }

    private static TimeZoneInfo FindLondon()
    {
        foreach (var id in new[] { "Europe/London", "GMT Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)) return zone;
        }

        throw new InvalidOperationException("The UK time zone (Europe/London) wasn't found on this server.");
    }
}
