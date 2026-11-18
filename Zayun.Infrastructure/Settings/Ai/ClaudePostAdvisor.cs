using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Zayun.Domain.Interfaces;

namespace Zayun.Infrastructure.Settings.Ai;

/// <summary>
/// <see cref="IPostAdvisor"/> with Claude, through the shared <see cref="ClaudeMessages"/> connection (the same one the
/// tag check and the quote helper use). One short request when the admin presses "Draft a post".
/// </summary>
/// <remarks>
/// <para>Never throws for the AI: no key, a timeout, a refusal or an unreadable answer is logged and returns null.</para>
/// <para>Only the item's own facts and topics are sent: no personal data.</para>
/// </remarks>
internal sealed class ClaudePostAdvisor(ClaudeMessages claude, ILogger<ClaudePostAdvisor> logger) : IPostAdvisor
{
    private const string Helper = "post helper";

    private const string Instructions =
        """
        You help the owner of a personal website (a software engineer and AI researcher) share an item from the site
        on LinkedIn, WhatsApp and X. He posts it himself, in his own name. You write only the words AROUND the item:
        the item itself (for a quote: the words, who said it and the link) is added after your text by the website,
        exactly as saved. So never repeat, reword or translate the quote's words, and never add a link.

        Write three things:
        1. "long_intro": for LinkedIn. One to three short, plain sentences (at most 350 characters) in British English,
           in the owner's voice ("I", "me" are fine, sparingly). Say why the item is worth a moment: the idea behind
           it, or the true story given in the facts. Calm and professional, warm, not salesy.
        2. "short_intro": for WhatsApp or X. One sentence, at most 110 characters.
        3. "hashtags": three to five hashtags without the "#", each in CamelCase with letters and digits only (for
           example "Biomimicry", "ArtificialIntelligence", "NigerianProverbs", "ScienceHistory"). Base them on the
           topics given; prefer tags people really search for; no names of living people.

        Rules for both intros:
        - Use ONLY the facts you are given. Never add dates, places, events, numbers or claims that are not in the
          input, and never call a saying "famous" or "well known" unless the input says so.
        - For the owner's own saying, don't claim it is anything more than his own words.
        - For a proverb, you may say whose proverb it is, as given; don't explain its meaning beyond the translation.
        - No emojis, no hashtags, no links, no exclamation marks, no quotation marks, no "Quote of the day" opener,
          no questions to the reader like "What do you think?".
        """;

    /// <inheritdoc />
    public bool IsAvailable => claude.IsAvailable;

    /// <inheritdoc />
    public async Task<PostDraft?> DraftAsync(PostDraftRequest item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!claude.IsAvailable) return null;

        var json = await claude.AskForJsonAsync(Helper, Instructions, Describe(item), DraftSchema, maxTokens: 500,
                                                cancellationToken);
        if (json is null) return null;

        try
        {
            var draft = JsonSerializer.Deserialize<Draft>(json, ClaudeMessages.Json);
            if (draft is null || string.IsNullOrWhiteSpace(draft.LongIntro) || string.IsNullOrWhiteSpace(draft.ShortIntro))
            {
                logger.LogWarning("AI post helper: the answer had no intro.");
                return null;
            }

            logger.LogInformation("AI post helper: drafted a post for {What} with {Count} hashtags",
                                  item.What, draft.Hashtags?.Count ?? 0);
            return new PostDraft(draft.LongIntro.Trim(), draft.ShortIntro.Trim(), draft.Hashtags ?? []);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "AI post helper: the answer couldn't be read.");
            return null;
        }
    }

    /// <summary>The item, as the model reads it.</summary>
    private static string Describe(PostDraftRequest item)
    {
        var text = new StringBuilder();
        text.Append("Item: ").AppendLine(item.What);
        foreach (var fact in item.Facts.Where(f => !string.IsNullOrWhiteSpace(f.Value)))
            text.Append(fact.Label).Append(": ").AppendLine(fact.Value);

        text.AppendLine();
        text.AppendLine(item.Topics.Count == 0
            ? "Topics: none given."
            : $"Topics: {string.Join(", ", item.Topics)}");
        return text.ToString();
    }

    /// <summary>The JSON the model must answer with.</summary>
    private static readonly object DraftSchema = new
    {
        type = "object",
        properties = new
        {
            long_intro = new { type = "string" },
            short_intro = new { type = "string" },
            hashtags = new { type = "array", items = new { type = "string" } }
        },
        required = new[] { "long_intro", "short_intro", "hashtags" },
        additionalProperties = false
    };

    private sealed record Draft(
        [property: JsonPropertyName("long_intro")] string? LongIntro,
        [property: JsonPropertyName("short_intro")] string? ShortIntro,
        IReadOnlyList<string>? Hashtags);
}

/// <summary><see cref="IPostAdvisor"/> when AI is off: "Draft a post" answers that the helper is switched off.</summary>
internal sealed class NoPostAdvisor : IPostAdvisor
{
    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public Task<PostDraft?> DraftAsync(PostDraftRequest item, CancellationToken cancellationToken = default) =>
        Task.FromResult<PostDraft?>(null);
}
