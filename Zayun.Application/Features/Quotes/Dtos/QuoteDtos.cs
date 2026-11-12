using Zayun.Domain.Entities;
using Zayun.Domain.Enums;

namespace Zayun.Application.Features.Quotes.Dtos;

/// <summary>Today's quote, as the website shows it.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Date">The UK date it's the quote for.</param>
/// <param name="Kind">Quote, proverb or the owner's own saying.</param>
/// <param name="Text">The words, in their original language.</param>
/// <param name="Author">Who said it.</param>
/// <param name="Source">Where it comes from; null when not known.</param>
/// <param name="Language">The language when it isn't English; null for English.</param>
/// <param name="Translation">The English translation; null when it's already English.</param>
/// <param name="Series">A series, e.g. "Nature first"; null for none.</param>
/// <param name="Story">A one-line true story shown with it; null for none.</param>
public sealed record QuoteOfTheDayDto(
    Guid Id,
    DateOnly Date,
    QuoteKind Kind,
    string Text,
    string Author,
    string? Source,
    string? Language,
    string? Translation,
    string? Series,
    string? Story)
{
    /// <summary>Maps a quote for a date.</summary>
    /// <param name="q">The quote.</param>
    /// <param name="date">The UK date.</param>
    public static QuoteOfTheDayDto From(Quote q, DateOnly date) =>
        new(q.Id, date, q.Kind, q.Text, q.Author, q.Source, q.Language, q.Translation, q.Series, q.Story);
}
