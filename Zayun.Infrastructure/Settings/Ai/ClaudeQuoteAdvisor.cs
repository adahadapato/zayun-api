using System.Text.Json;
using Microsoft.Extensions.Logging;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;

namespace Zayun.Infrastructure.Settings.Ai;

/// <summary>
/// <see cref="IQuoteAdvisor"/> with Claude, through the shared <see cref="ClaudeMessages"/> connection (the same one the
/// tag check uses): one short request when the admin presses "Suggest with AI"; the answer is JSON in a fixed shape
/// (structured outputs).
/// </summary>
/// <remarks>
/// <para>Never throws for the AI: no key, a timeout, a refusal or an unreadable answer is logged and returns null.</para>
/// <para>Only the quote's own fields and the existing theme names are sent: no personal data.</para>
/// <para>The AI is told to use only the facts it's given (who said it, the source, the story), never to add dates,
/// places or claims of its own: the description appears in Google next to the owner's name.</para>
/// </remarks>
internal sealed class ClaudeQuoteAdvisor(ClaudeMessages claude, ILogger<ClaudeQuoteAdvisor> logger) : IQuoteAdvisor
{
    private const string Helper = "quote helper";

    private const string Instructions =
        """
        You help the owner of a personal website (a software engineer and AI researcher) publish a "quote of the
        day". Each quote has its own page. For the quote you are given, write two things.

        1. "description": the text Google shows under the page's title and that LinkedIn / WhatsApp show in link
           previews. Between 110 and 155 characters, one or two plain sentences in British English. Say who said it
           and what the quote is about or why it matters, so a reader wants to open the page. Use ONLY the facts you
           are given (who said it, the source, the translation, the story): never add dates, places, events or claims
           that are not in the input, and never call a saying "famous" unless the input says so. No emojis, no
           hashtags, no exclamation marks, no quotation marks around the whole description. Don't start with "This
           quote". For a proverb, say which people's proverb it is and give its meaning in English. For the owner's
           own saying, name the owner as the author.

        2. "themes": one to three short themes for grouping related quotes, each one or two words in Title Case
           (for example "Nature", "Flight", "Evolution", "Artificial Intelligence", "Curiosity", "Honesty",
           "Computing", "Wonder"). Strongly prefer themes from the existing list, spelled EXACTLY as listed; only
           add a new theme when none of the existing ones fits. Themes describe ideas, not people or sources: no
           author names, book titles or languages.
        """;

    /// <inheritdoc />
    public bool IsAvailable => claude.IsAvailable;

    /// <inheritdoc />
    public async Task<QuoteAdvice?> SuggestAsync(QuoteAdviceRequest quote, IReadOnlyList<string> existingThemes,
                                                 CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(quote);
        if (!claude.IsAvailable) return null;

        var json = await claude.AskForJsonAsync(Helper, Instructions, Describe(quote, existingThemes), AdviceSchema,
                                                maxTokens: 400, cancellationToken);
        if (json is null) return null;

        try
        {
            var advice = JsonSerializer.Deserialize<Advice>(json, ClaudeMessages.Json);
            if (advice is null || string.IsNullOrWhiteSpace(advice.Description))
            {
                logger.LogWarning("AI quote helper: the answer had no description.");
                return null;
            }

            logger.LogInformation("AI quote helper: drafted a description and {Count} themes for a quote by {Author}",
                                  advice.Themes?.Count ?? 0, quote.Author);
            return new QuoteAdvice(advice.Description.Trim(), advice.Themes ?? []);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "AI quote helper: the answer couldn't be read.");
            return null;
        }
    }

    /// <summary>The quote and the existing themes, as the model reads them.</summary>
    private static string Describe(QuoteAdviceRequest q, IReadOnlyList<string> existingThemes)
    {
        var kind = q.Kind switch
        {
            QuoteKind.Proverb => "a traditional proverb",
            QuoteKind.OwnSaying => "the website owner's own saying",
            _ => "a quote"
        };

        var lines = new List<string>
        {
            $"Kind: {kind}",
            $"Words: \"{q.Text}\"",
            $"Who said it: {q.Author}"
        };
        if (!string.IsNullOrWhiteSpace(q.Language)) lines.Add($"Language: {q.Language}");
        if (!string.IsNullOrWhiteSpace(q.Translation)) lines.Add($"English translation: \"{q.Translation}\"");
        if (!string.IsNullOrWhiteSpace(q.Source)) lines.Add($"Source: {q.Source}");
        if (!string.IsNullOrWhiteSpace(q.Story)) lines.Add($"Story shown with it: {q.Story}");

        lines.Add("");
        lines.Add(existingThemes.Count == 0
            ? "Existing themes: none yet."
            : $"Existing themes ({existingThemes.Count}):\n{string.Join("\n", existingThemes.Select(t => "- " + t))}");

        return string.Join("\n", lines);
    }

    /// <summary>The JSON the model must answer with.</summary>
    private static readonly object AdviceSchema = new
    {
        type = "object",
        properties = new
        {
            description = new { type = "string" },
            themes = new { type = "array", items = new { type = "string" } }
        },
        required = new[] { "description", "themes" },
        additionalProperties = false
    };

    private sealed record Advice(string? Description, IReadOnlyList<string>? Themes);
}

/// <summary><see cref="IQuoteAdvisor"/> when AI is off: "Suggest with AI" answers that the helper is switched off.</summary>
internal sealed class NoQuoteAdvisor : IQuoteAdvisor
{
    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public Task<QuoteAdvice?> SuggestAsync(QuoteAdviceRequest quote, IReadOnlyList<string> existingThemes,
                                           CancellationToken cancellationToken = default) =>
        Task.FromResult<QuoteAdvice?>(null);
}
