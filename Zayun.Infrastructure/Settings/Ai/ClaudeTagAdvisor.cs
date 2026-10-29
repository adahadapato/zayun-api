using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Text;

namespace Zayun.Infrastructure.Settings.Ai;

/// <summary>
/// <see cref="ITagAdvisor"/> with Claude (the Anthropic Messages API). One short request per NEW tag name the
/// built-in rules don't know; the answer is JSON in a fixed shape (structured outputs).
/// </summary>
/// <remarks>
/// <para>Never throws for the AI: no key, a timeout, a refusal or an unreadable answer is logged and returns null, so
/// the name is simply treated as new.</para>
/// <para>Only the typed name and the existing tag names are sent: no personal data.</para>
/// </remarks>
internal sealed class ClaudeTagAdvisor(HttpClient http, IOptions<AiSettings> options, ILogger<ClaudeTagAdvisor> logger)
    : ITagAdvisor
{
    private const string AnthropicVersion = "2023-06-01";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string Instructions =
        """
        You keep the tag list of a portfolio website consistent. The site belongs to a software engineer and AI
        researcher; tags name technologies, programming languages, frameworks, tools, methods, research topics and
        professional skills (for example "ASP.NET Core", "PyTorch", "Explainable AI", "Technical Leadership").

        You get the existing tags and one NEW name the admin typed. Decide:
        - "same_as_existing": the new name means the same as one existing tag: a synonym, abbreviation, plural,
          different spelling or wording ("RESTful services" = "REST API", "EF Core" = "Entity Framework Core").
          Put that existing tag in "name", spelled EXACTLY as in the list. Only when it really is the same thing:
          "React" and "React Native" are different, so are "Java" and "JavaScript", ".NET" and ".NET MAUI".
        - "new_tag": a real skill, technology or topic not in the list. Put its usual official spelling and casing
          in "name" ("typescript" → "TypeScript", "pytroch" → "PyTorch", "asp.net core" → "ASP.NET Core"), fix
          obvious typos, keep it short (under 60 characters), no trailing punctuation. Keep the admin's wording when
          it's already correct.
        - "not_a_tag": not a skill, technology or topic (random letters, a sentence, a person's name, something
          unrelated or offensive). Put the typed name in "name".
        "reason": one short, friendly sentence for the admin (under 15 words).
        """;

    /// <inheritdoc />
    public bool IsAvailable => options.Value.IsUsable;

    /// <inheritdoc />
    public async Task<TagAdvice?> AdviseAsync(string name, IReadOnlyList<string> existingTags,
                                              CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsUsable || string.IsNullOrWhiteSpace(name)) return null;

        var request = new MessagesRequest(
            Model: settings.Model,
            MaxTokens: 300,
            System: Instructions,
            Messages:
            [
                new Message("user",
                    $"Existing tags ({existingTags.Count}):\n{string.Join("\n", existingTags.Select(t => "- " + t))}" +
                    $"\n\nNew name typed by the admin: \"{name}\"")
            ],
            OutputConfig: new OutputConfig(new OutputFormat("json_schema", VerdictSchema)));

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
                logger.LogWarning("AI tag check failed: {Status} {Body}", (int)response.StatusCode,
                                  body.Length > 500 ? body[..500] : body);
                return null;
            }

            var answer = await response.Content.ReadFromJsonAsync<MessagesResponse>(Json, timeout.Token);
            if (answer is null || answer.StopReason is "refusal" or "max_tokens")
            {
                logger.LogWarning("AI tag check gave no usable answer ({StopReason}).", answer?.StopReason);
                return null;
            }

            var text = answer.Content?.FirstOrDefault(c => c.Type == "text")?.Text;
            if (string.IsNullOrWhiteSpace(text)) return null;

            var verdict = JsonSerializer.Deserialize<Verdict>(text, Json);
            if (verdict is null || string.IsNullOrWhiteSpace(verdict.Name)) return null;

            var kind = verdict.Decision?.Trim().ToLowerInvariant() switch
            {
                "same_as_existing" => TagAdviceVerdict.SameAsExisting,
                "new_tag" => TagAdviceVerdict.NewTag,
                "not_a_tag" => TagAdviceVerdict.NotATag,
                _ => (TagAdviceVerdict?)null
            };
            if (kind is null) return null;

            var advisedName = TagNames.Clean(verdict.Name);
            logger.LogInformation("AI tag check: \"{Name}\" → {Verdict} \"{Advised}\"", name, kind, advisedName);
            return new TagAdvice(kind.Value, advisedName, verdict.Reason?.Trim());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("AI tag check timed out after {Seconds}s.", settings.TimeoutSeconds);
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            logger.LogWarning(ex, "AI tag check failed.");
            return null;
        }
    }

    // ───────────────────────────── Request and answer shapes ─────────────────────────────

    /// <summary>The JSON the model must answer with.</summary>
    private static readonly object VerdictSchema = new
    {
        type = "object",
        properties = new
        {
            decision = new { type = "string", @enum = new[] { "same_as_existing", "new_tag", "not_a_tag" } },
            name = new { type = "string" },
            reason = new { type = "string" }
        },
        required = new[] { "decision", "name", "reason" },
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

    private sealed record Verdict(string? Decision, string? Name, string? Reason);
}

/// <summary><see cref="ITagAdvisor"/> when AI is off: never asked, so only the built-in rules are used.</summary>
internal sealed class NoTagAdvisor : ITagAdvisor
{
    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public Task<TagAdvice?> AdviseAsync(string name, IReadOnlyList<string> existingTags,
                                        CancellationToken cancellationToken = default) =>
        Task.FromResult<TagAdvice?>(null);
}
