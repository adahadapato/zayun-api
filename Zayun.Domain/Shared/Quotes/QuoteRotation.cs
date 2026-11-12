using Zayun.Domain.Entities;

namespace Zayun.Domain.Shared.Quotes;

/// <summary>
/// Picks the quote of the day. The same date always gives the same quote, so everyone sees the same one all day.
/// </summary>
/// <remarks>
/// <para><b>1. Only published, source-checked quotes</b> can be shown.</para>
/// <para><b>2. A quote pinned to today's day and month wins</b> (the first one in order, if several share a day).
/// Pinned quotes are shown on their day only.</para>
/// <para><b>3. On <see cref="SeriesDay"/> (Sunday):</b> a quote from a series (e.g. "Nature first"), going through the
/// series quotes one week at a time.</para>
/// <para><b>4. Every other day:</b> the everyday rotation (quotes not pinned and not in a series), in display order.
/// Count the days since <see cref="Start"/>: day 0 shows the first quote, day 1 the second, and after the last one it
/// starts again. Nothing repeats until the whole list has been used.</para>
/// <para>If one of the two lists is empty, the other one is used every day.</para>
/// <para>Adding, removing or reordering quotes shifts which one falls on which day; that's fine for a daily quote.</para>
/// </remarks>
public static class QuoteRotation
{
    /// <summary>Day 0 of the rotation (a Thursday).</summary>
    public static readonly DateOnly Start = new(2026, 1, 1);

    /// <summary>The day of the week series quotes (e.g. "Nature first") are shown.</summary>
    public const DayOfWeek SeriesDay = DayOfWeek.Sunday;

    /// <summary>The quote for a date.</summary>
    /// <param name="quotes">Every published quote, in display order.</param>
    /// <param name="date">The date (UK time).</param>
    /// <returns>The quote, or null when none can be shown.</returns>
    public static Quote? Pick(IReadOnlyList<Quote> quotes, DateOnly date) => PickWithReason(quotes, date)?.Quote;

    /// <summary>The quote for a date, and why it was chosen (for the admin's preview).</summary>
    /// <param name="quotes">Every quote, in display order (drafts and unchecked ones are skipped).</param>
    /// <param name="date">The date (UK time).</param>
    /// <returns>The pick, or null when none can be shown.</returns>
    public static QuotePick? PickWithReason(IReadOnlyList<Quote> quotes, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(quotes);

        var showable = quotes.Where(IsShowable).ToList();

        var pinned = showable.FirstOrDefault(q => q.IsPinnedTo(date));
        if (pinned is not null) return new QuotePick(pinned, QuotePickReason.PinnedDay);

        var series = showable.Where(q => !q.IsPinned && q.Series is not null).ToList();
        var everyday = showable.Where(q => !q.IsPinned && q.Series is null).ToList();
        var days = date.DayNumber - Start.DayNumber;

        if (series.Count > 0 && (date.DayOfWeek == SeriesDay || everyday.Count == 0))
        {
            // Week by week on the series day; day by day only when there's nothing else to show
            var index = everyday.Count == 0 ? days : FloorDivide(days, 7);
            return new QuotePick(series[Wrap(index, series.Count)], QuotePickReason.WeeklySeries);
        }

        return everyday.Count == 0
            ? null
            : new QuotePick(everyday[Wrap(days, everyday.Count)], QuotePickReason.Rotation);
    }

    /// <summary>True when the website may show it: published and its source checked.</summary>
    /// <param name="quote">The quote.</param>
    public static bool IsShowable(Quote quote) => quote.IsPublished && quote.IsSourceChecked;

    /// <summary>A position in a list of <paramref name="count"/> items, never negative (even before <see cref="Start"/>).</summary>
    private static int Wrap(int value, int count) => ((value % count) + count) % count;

    /// <summary>Division that rounds down for negative numbers too (-1 / 7 = -1, not 0).</summary>
    private static int FloorDivide(int value, int by) => value >= 0 ? value / by : -((-value + by - 1) / by);
}

/// <summary>The quote chosen for a day and why.</summary>
/// <param name="Quote">The quote.</param>
/// <param name="Reason">Why it was chosen.</param>
public sealed record QuotePick(Quote Quote, QuotePickReason Reason);

/// <summary>Why a quote was chosen for a day.</summary>
public enum QuotePickReason
{
    /// <summary>It's pinned to that day of the year.</summary>
    PinnedDay = 1,

    /// <summary>It's the series day (Sunday), and it's that week's series quote.</summary>
    WeeklySeries = 2,

    /// <summary>It's that day's place in the everyday rotation.</summary>
    Rotation = 3
}
