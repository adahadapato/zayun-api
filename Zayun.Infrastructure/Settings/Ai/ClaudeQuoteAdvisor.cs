using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;

namespace Zayun.Infrastructure.Settings.Ai;

/// <summary>
/// <see cref="IQuoteAdvisor"/> with Claude (the Anthropic Messages API), built like <see cref="ClaudeTagAdvisor"/>:
/// one short request when the admin presses "Suggest with AI"; the answer is JSON in a fixed shape (structured
/// outputs).
/// </summary>
/// <remarks>
/// <para>Never throws for the AI: no key, a timeout, a refusal or an unreadable answer is logged and returns null.</para>
/// <para>Only the quote's own fields and the existing theme names are sent: no personal data.</para>
/// <para>The AI is told to use only the facts it's given (who said it, the source, the story), never to add dates,
/// places or claims of its own: the description appears in Google next to the owner's name.</para>
/// </remarks>
internal sealed class ClaudeQuoteAdvisor(HttpClient http, IOptions<AiSettings> options,
                                         ILogger<ClaudeQuoteAdvisor> logger) : IQuoteAdvisor
{
    private const string AnthropicVersion = "2023-06-01";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

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
    public bool IsAvailable => options.Value.IsUsable;

    /// <inheritdoc />
    public async Task<QuoteAdvice?> SuggestAsync(QuoteAdviceRequest quote, IReadOnlyList<string> existingThemes,
                                                 CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(quote);
        var settings = options.Value;
        if (!settings.IsUsable) return null;

        var request = new MessagesRequest(
            Model: settings.Model,
            MaxTokens: 400,
            System: Instructions,
            Messages: [new Message("user", Describe(quote, existingThemes))],
            OutputConfig: new OutputConfig(new OutputFormat("json_schema", AdviceSchema)));

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

            using var message = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
            {
                Content = JsonContent.Create(request, options: Json)
            };
            message.Headers.Add("x-api-key", settings.ApiKey);
            message.Headers.Add("anthropic-version", AnthropicVersion);

            using var response = await http.SendAsync(message, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(timeout.Token);
                logger.LogWarning("AI quote helper failed: {Status} {Body}", (int)response.StatusCode,
                                  body.Length > 500 ? body[..500] : body);
                return null;
            }

            var answer = await response.Content.ReadFromJsonAsync<MessagesResponse>(Json, timeout.Token);
            if (answer is null || answer.StopReason is "refusal" or "max_tokens")
            {
                logger.LogWarning("AI quote helper gave no usable answer ({StopReason}).", answer?.StopReason);
                return null;
            }

            var text = answer.Content?.FirstOrDefault(c => c.Type == "text")?.Text;
            if (string.IsNullOrWhiteSpace(text)) return null;

            var advice = JsonSerializer.Deserialize<Advice>(text, Json);
            if (advice is null || string.IsNullOrWhiteSpace(advice.Description)) return null;

            logger.LogInformation("AI quote helper: drafted a description and {Count} themes for a quote by {Author}",
                                  advice.Themes?.Count ?? 0, quote.Author);
            return new QuoteAdvice(advice.Description.Trim(), advice.Themes ?? []);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("AI quote helper timed out after {Seconds}s.", settings.TimeoutSeconds);
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            logger.LogWarning(ex, "AI quote helper failed.");
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

    // ───────────────────────────── Request and answer shapes ─────────────────────────────

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

    private sealed record MessagesRequest(
        string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        string System,
        IReadOnlyList<Message> Messages,
        [property: JsonPropertyName("output_config")] OutputConfig OutputConfig);

    private sealed record Message(string Role, string Content);

    private sealed record OutputConfig(OutputFormat Format);

    private sealed record OutputFormat(string Type, object Schema);

    private sealed record MessagesResponse(
        IReadOnlyList<ContentBlock>? Content,
        [property: JsonPropertyName("stop_reason")] string? StopReason);

    private sealed record ContentBlock(string Type, string? Text);

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
