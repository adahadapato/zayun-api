using Zayun.Domain.Enums;

namespace Zayun.Domain.Interfaces;

/// <summary>
/// The AI helper in Admin → Quotes: drafts a quote's description for Google and link previews, and suggests its
/// themes (reusing themes other quotes already have). Only drafts: the admin edits them and saves; nothing is saved
/// or shown on the website by the AI itself. Implemented in Infrastructure (Claude); when AI is switched off,
/// <see cref="IsAvailable"/> is false.
/// </summary>
public interface IQuoteAdvisor
{
    /// <summary>True when the AI can be used (switched on, with a key).</summary>
    bool IsAvailable { get; }

    /// <summary>Drafts the description and themes for one quote.</summary>
    /// <param name="quote">The quote as typed in the form (it may not be saved yet).</param>
    /// <param name="existingThemes">Every theme other quotes use, so the AI reuses them.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The drafts, or null when the AI couldn't be asked (no key, timeout, error): never throws for that.</returns>
    Task<QuoteAdvice?> SuggestAsync(QuoteAdviceRequest quote, IReadOnlyList<string> existingThemes,
                                    CancellationToken cancellationToken = default);
}

/// <summary>What the AI is told about the quote.</summary>
/// <param name="Kind">Quote, proverb or the owner's own saying.</param>
/// <param name="Text">The words.</param>
/// <param name="Author">Who said it.</param>
/// <param name="Source">Where it comes from (optional).</param>
/// <param name="Language">The language when it isn't English (optional).</param>
/// <param name="Translation">The English translation (optional).</param>
/// <param name="Story">The one-line story (optional).</param>
public sealed record QuoteAdviceRequest(
    QuoteKind Kind,
    string Text,
    string Author,
    string? Source,
    string? Language,
    string? Translation,
    string? Story);

/// <summary>The AI's drafts.</summary>
/// <param name="SearchDescription">A description for Google and link previews.</param>
/// <param name="Themes">One to three themes.</param>
public sealed record QuoteAdvice(string SearchDescription, IReadOnlyList<string> Themes);
