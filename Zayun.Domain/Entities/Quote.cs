using Zayun.Domain.Enums;
using Zayun.Domain.Shared.Text;

namespace Zayun.Domain.Entities;

/// <summary>
/// A quote, proverb or saying for the "Quote of the day" card on the website, managed in Admin → Quotes.
/// </summary>
/// <remarks>
/// <para><b>Published</b> (from <see cref="ContentEntity"/>): only published quotes can be shown.</para>
/// <para><b>Source checked:</b> a quote is only shown once its wording and who said it have been checked, because
/// misattributed quotes are everywhere online. The owner's own sayings are simply ticked.</para>
/// <para><b>Pinned day:</b> a quote can be pinned to a day of the year (e.g. 1 October). It is then shown on that day
/// every year, and on no other day.</para>
/// <para><b>Display order</b> (from <see cref="ContentEntity"/>) is the order of the daily rotation.</para>
/// <para>Bad text is caught by the validators first, so the checks here THROW.</para>
/// </remarks>
public class Quote : ContentEntity
{
    /// <summary>Longest quote text.</summary>
    public const int TextMaxLength = 500;

    /// <summary>Longest "who said it", e.g. "Ada Lovelace" or "Annang proverb".</summary>
    public const int AuthorMaxLength = 120;

    /// <summary>Longest source, e.g. "Notes on the Analytical Engine (1843)".</summary>
    public const int SourceMaxLength = 300;

    /// <summary>Longest language name, e.g. "Annang".</summary>
    public const int LanguageMaxLength = 60;

    /// <summary>Longest English translation.</summary>
    public const int TranslationMaxLength = 500;

    /// <summary>Longest series name, e.g. "Nature first".</summary>
    public const int SeriesMaxLength = 60;

    /// <summary>Longest story line under an illustrated saying.</summary>
    public const int StoryMaxLength = 300;

    /// <summary>Required by EF Core.</summary>
    protected Quote() { }

    private Quote(Guid id, int displayOrder, bool isPublished) : base(id, displayOrder, isPublished) { }

    /// <summary>Quote, proverb or the owner's own saying.</summary>
    public QuoteKind Kind { get; private set; }

    /// <summary>The words, in their original language.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>Who said it, e.g. "Ada Lovelace", "Annang proverb" or "Enobong Adahada".</summary>
    public string Author { get; private set; } = string.Empty;

    /// <summary>Where it comes from, e.g. a book, paper or speech with its year; null when not known.</summary>
    public string? Source { get; private set; }

    /// <summary>The language of <see cref="Text"/> when it isn't English, e.g. "Annang"; null for English.</summary>
    public string? Language { get; private set; }

    /// <summary>The English translation when <see cref="Text"/> isn't in English; null otherwise.</summary>
    public string? Translation { get; private set; }

    /// <summary>A series it belongs to, e.g. "Nature first"; null for none.</summary>
    public string? Series { get; private set; }

    /// <summary>A one-line true story shown with it, e.g. how the Wright brothers learned from birds; null for none.</summary>
    public string? Story { get; private set; }

    /// <summary>True once the wording and who said it have been checked. Only checked quotes are shown.</summary>
    public bool IsSourceChecked { get; private set; }

    /// <summary>The month (1–12) it's pinned to, or null when not pinned.</summary>
    public int? PinnedMonth { get; private set; }

    /// <summary>The day of the month it's pinned to, or null when not pinned.</summary>
    public int? PinnedDay { get; private set; }

    /// <summary>True when it's pinned to a day of the year.</summary>
    public bool IsPinned => PinnedMonth is not null && PinnedDay is not null;

    /// <summary>True when it's pinned to this date's day and month (any year).</summary>
    /// <param name="date">The date.</param>
    public bool IsPinnedTo(DateOnly date) => PinnedMonth == date.Month && PinnedDay == date.Day;

    /// <summary>Adds a quote.</summary>
    /// <param name="details">Everything about it except its place.</param>
    /// <param name="displayOrder">Its place in the rotation.</param>
    /// <param name="isPublished">Shown straight away, or kept as a draft.</param>
    /// <returns>The new quote.</returns>
    /// <exception cref="ArgumentException">Missing text, an unknown kind, or a translation without a language.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, or a pinned day that doesn't exist.</exception>
    public static Quote Create(QuoteDetails details, int displayOrder, bool isPublished)
    {
        var quote = new Quote(Guid.CreateVersion7(), displayOrder, isPublished);
        quote.Update(details);
        return quote;
    }

    /// <summary>Changes everything except its place.</summary>
    /// <param name="details">The new details.</param>
    /// <exception cref="ArgumentException">Missing text, an unknown kind, or a translation without a language.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, or a pinned day that doesn't exist.</exception>
    public void Update(QuoteDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        if (!Enum.IsDefined(details.Kind))
            throw new ArgumentException("Unknown quote kind.", nameof(details));

        var language = TextRules.Optional(details.Language, LanguageMaxLength, nameof(details.Language));
        var translation = TextRules.Optional(details.Translation, TranslationMaxLength, nameof(details.Translation));
        if (translation is not null && language is null)
            throw new ArgumentException("A translation needs the original language.", nameof(details));

        Kind = details.Kind;
        Text = TextRules.Required(details.Text, TextMaxLength, nameof(details.Text));
        Author = TextRules.Required(details.Author, AuthorMaxLength, nameof(details.Author));
        Source = TextRules.Optional(details.Source, SourceMaxLength, nameof(details.Source));
        Language = language;
        Translation = translation;
        Series = TextRules.Optional(details.Series, SeriesMaxLength, nameof(details.Series));
        Story = TextRules.Optional(details.Story, StoryMaxLength, nameof(details.Story));
        IsSourceChecked = details.IsSourceChecked;
        (PinnedMonth, PinnedDay) = CheckPinnedDay(details.PinnedMonth, details.PinnedDay);
    }

    /// <summary>Both null (not pinned), or a real day of the year. 29 February is allowed (shown in leap years).</summary>
    private static (int? Month, int? Day) CheckPinnedDay(int? month, int? day)
    {
        if (month is null && day is null) return (null, null);
        if (month is null || day is null)
            throw new ArgumentException("A pinned day needs both the month and the day.", nameof(day));

        ArgumentOutOfRangeException.ThrowIfLessThan(month.Value, 1, nameof(month));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month.Value, 12, nameof(month));
        ArgumentOutOfRangeException.ThrowIfLessThan(day.Value, 1, nameof(day));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(day.Value, DateTime.DaysInMonth(2024, month.Value), nameof(day));
        return (month, day);
    }
}

/// <summary>Everything about a quote an admin types in.</summary>
/// <param name="Kind">Quote, proverb or the owner's own saying.</param>
/// <param name="Text">The words, in their original language.</param>
/// <param name="Author">Who said it.</param>
/// <param name="Source">Where it comes from (optional).</param>
/// <param name="Language">The language when it isn't English (optional).</param>
/// <param name="Translation">The English translation (optional; needs <paramref name="Language"/>).</param>
/// <param name="Series">A series, e.g. "Nature first" (optional).</param>
/// <param name="Story">A one-line true story shown with it (optional).</param>
/// <param name="IsSourceChecked">The wording and who said it have been checked.</param>
/// <param name="PinnedMonth">The month it's pinned to (optional; with <paramref name="PinnedDay"/>).</param>
/// <param name="PinnedDay">The day it's pinned to (optional; with <paramref name="PinnedMonth"/>).</param>
public sealed record QuoteDetails(
    QuoteKind Kind,
    string Text,
    string Author,
    string? Source,
    string? Language,
    string? Translation,
    string? Series,
    string? Story,
    bool IsSourceChecked,
    int? PinnedMonth,
    int? PinnedDay);
