using MediatR;
using Zayun.Application.Features.Quotes.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Quotes.Queries;

/// <summary>
/// Admin → Quotes → "Suggest with AI": drafts the Google description and the themes for the quote being edited (it may
/// not be saved yet, so the form's fields are sent). Saves nothing. Never cached.
/// </summary>
/// <param name="Kind">Quote, proverb or the owner's own saying.</param>
/// <param name="Text">The words.</param>
/// <param name="Author">Who said it.</param>
/// <param name="Source">Where it comes from.</param>
/// <param name="Language">The language when it isn't English.</param>
/// <param name="Translation">The English translation.</param>
/// <param name="Story">The one-line story.</param>
public sealed record SuggestQuoteDetailsQuery(
    QuoteKind Kind,
    string Text,
    string Author,
    string? Source,
    string? Language,
    string? Translation,
    string? Story) : IRequest<Result<QuoteSuggestionDto>>;

/// <summary>Handles <see cref="SuggestQuoteDetailsQuery"/>.</summary>
/// <remarks>
/// <para>The themes other quotes already use are sent along (most used first), so the AI reuses them and the list
/// stays small and consistent. A suggested theme that matches an existing one in another letter case gets the
/// existing spelling.</para>
/// <para>The answer is tidied to fit the rules (description length, number and length of themes), so the drafts can
/// always be saved as they are.</para>
/// </remarks>
public sealed class SuggestQuoteDetailsQueryHandler(IQuoteRepository quotes, IQuoteAdvisor advisor)
    : IRequestHandler<SuggestQuoteDetailsQuery, Result<QuoteSuggestionDto>>
{
    /// <summary>Most themes suggested at once (the admin can add more, up to <see cref="Quote.MaxThemes"/>).</summary>
    public const int MaxSuggestedThemes = 3;

    /// <inheritdoc />
    public async Task<Result<QuoteSuggestionDto>> Handle(SuggestQuoteDetailsQuery query, CancellationToken ct)
    {
        if (!advisor.IsAvailable) return QuoteErrors.AiUnavailable;

        var existingThemes = (await quotes.GetListAsync(publishedOnly: false, ct))
            .SelectMany(q => q.Themes)
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var advice = await advisor.SuggestAsync(
            new QuoteAdviceRequest(query.Kind, query.Text, query.Author, query.Source, query.Language,
                                   query.Translation, query.Story),
            existingThemes, ct);
        if (advice is null || string.IsNullOrWhiteSpace(advice.SearchDescription)) return QuoteErrors.AiNoAnswer;

        var byKey = existingThemes.ToDictionary(t => t, StringComparer.OrdinalIgnoreCase);
        var themes = advice.Themes
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().TrimEnd('.', ',', ';'))
            .Where(t => t.Length is > 0 and <= Quote.ThemeMaxLength)
            .Select(t => byKey.TryGetValue(t, out var existing) ? existing : t)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSuggestedThemes)
            .ToList();

        return new QuoteSuggestionDto(Shorten(advice.SearchDescription, Quote.SearchDescriptionMaxLength), themes);
    }

    /// <summary>Trims the text and, when too long, cuts it at a word and ends it with "…".</summary>
    private static string Shorten(string text, int maxLength)
    {
        var clean = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (clean.Length <= maxLength) return clean;

        var cut = clean[..(maxLength - 1)];
        var lastSpace = cut.LastIndexOf(' ');
        return (lastSpace > maxLength / 2 ? cut[..lastSpace] : cut).TrimEnd(',', ';', ':', '.', ' ') + "…";
    }
}
