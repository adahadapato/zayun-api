using Zayun.Domain.Entities;

namespace Zayun.Domain.Shared.Quotes;

/// <summary>
/// Picks the quote of the day. The same date always gives the same quote, so everyone sees the same one all day.
/// </summary>
/// <remarks>
/// <para><b>1. Only published, source-checked quotes</b> can be shown.</para>
/// <para><b>2. A quote pinned to today's day and month wins</b> (the first one in order, if several share a day).</para>
/// <para><b>3. Otherwise the rotation:</b> the quotes that aren't pinned, in display order. Count the days since
/// <see cref="Start"/>: day 0 shows the first quote, day 1 the second, and after the last one it starts again. Nothing
/// repeats until the whole list has been used.</para>
/// <para>Adding, removing or reordering quotes shifts which one falls on which day; that's fine for a daily quote.</para>
/// </remarks>
public static class QuoteRotation
{
    /// <summary>Day 0 of the rotation.</summary>
    public static readonly DateOnly Start = new(2026, 1, 1);

    /// <summary>The quote for a date.</summary>
    /// <param name="quotes">Every published quote, in display order.</param>
    /// <param name="date">The date (UK time).</param>
    /// <returns>The quote, or null when none can be shown.</returns>
    public static Quote? Pick(IReadOnlyList<Quote> quotes, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(quotes);

        var showable = quotes.Where(q => q.IsPublished && q.IsSourceChecked).ToList();

        var pinned = showable.FirstOrDefault(q => q.IsPinnedTo(date));
        if (pinned is not null) return pinned;

        var rotation = showable.Where(q => !q.IsPinned).ToList();
        if (rotation.Count == 0) return null;

        var days = date.DayNumber - Start.DayNumber;
        var index = ((days % rotation.Count) + rotation.Count) % rotation.Count;   // never negative, even before Start
        return rotation[index];
    }
}
