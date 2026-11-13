using Zayun.Domain.Entities;

namespace Zayun.Domain.Shared.Quotes;

/// <summary>
/// Which quotes the website's quote pages (<c>/quotes</c> and <c>/quotes/{slug}</c>) may show, and when each was last
/// the quote of the day.
/// </summary>
/// <remarks>
/// <para><b>The rule (agreed 13 Nov 2026, "option B"):</b></para>
/// <list type="bullet">
/// <item>Only quotes the daily card may show: published and source checked (<see cref="QuoteRotation.IsShowable"/>).
/// Drafts and unchecked quotes never appear.</item>
/// <item><b>Everyday quotes</b> (not pinned, not in a series) appear straight away.</item>
/// <item><b>Pinned quotes and series quotes</b> (e.g. "Nature first") stay hidden until they've been the quote of the
/// day at least once since <see cref="Start"/>, so their day stays a surprise.</item>
/// </list>
/// <para>Uses exactly the same daily rule as the card (<see cref="QuoteRotation"/>), going day by day from
/// <see cref="Start"/> to today. That's at most a few hundred days a year: cheap, and the result is cached.</para>
/// </remarks>
public static class QuoteArchive
{
    /// <summary>The day the quote of the day went live on zayun.net. Days before don't count as "shown".</summary>
    public static readonly DateOnly Start = new(2026, 11, 12);

    /// <summary>The quotes for the website's quote pages: the ones shown most recently first, then the everyday quotes
    /// not shown yet, in rotation order.</summary>
    /// <param name="quotes">Every published quote, in display order.</param>
    /// <param name="today">Today's UK date.</param>
    /// <returns>The entries.</returns>
    public static IReadOnlyList<ArchivedQuote> Build(IReadOnlyList<Quote> quotes, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(quotes);

        var lastShown = new Dictionary<Guid, DateOnly>();
        for (var day = Start; day <= today; day = day.AddDays(1))
        {
            var pick = QuoteRotation.Pick(quotes, day);
            if (pick is not null) lastShown[pick.Id] = day;
        }

        return quotes
            .Where(QuoteRotation.IsShowable)
            .Select(q => new ArchivedQuote(q, lastShown.TryGetValue(q.Id, out var shown) ? shown : null))
            .Where(e => e.LastShown is not null || IsEveryday(e.Quote))
            .Select((entry, position) => (entry, position))   // keep rotation order for the ones not shown yet
            .OrderByDescending(x => x.entry.LastShown ?? DateOnly.MinValue)
            .ThenBy(x => x.position)
            .Select(x => x.entry)
            .ToList();
    }

    /// <summary>An everyday quote: neither pinned to a day nor part of a series.</summary>
    /// <param name="quote">The quote.</param>
    public static bool IsEveryday(Quote quote) => !quote.IsPinned && quote.Series is null;
}

/// <summary>A quote on the website's quote pages.</summary>
/// <param name="Quote">The quote.</param>
/// <param name="LastShown">The last UK date it was the quote of the day (since <see cref="QuoteArchive.Start"/>), or
/// null when it hasn't been yet.</param>
public sealed record ArchivedQuote(Quote Quote, DateOnly? LastShown);
