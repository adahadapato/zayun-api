using System.Text.Json;
using Microsoft.Extensions.Logging;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Text;

namespace Zayun.Infrastructure.Settings.Ai;

/// <summary>
/// <see cref="ITagAdvisor"/> with Claude, through the shared <see cref="ClaudeMessages"/> connection. One short request
/// per NEW tag name the built-in rules don't know; the answer is JSON in a fixed shape (structured outputs).
/// </summary>
/// <remarks>
/// <para>Never throws for the AI: no key, a timeout, a refusal or an unreadable answer is logged and returns null, so
/// the name is simply treated as new.</para>
/// <para>Only the typed name and the existing tag names are sent: no personal data.</para>
/// </remarks>
internal sealed class ClaudeTagAdvisor(ClaudeMessages claude, ILogger<ClaudeTagAdvisor> logger) : ITagAdvisor
{
    private const string Helper = "tag check";

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
    public bool IsAvailable => claude.IsAvailable;

    /// <inheritdoc />
    public async Task<TagAdvice?> AdviseAsync(string name, IReadOnlyList<string> existingTags,
                                              CancellationToken cancellationToken = default)
    {
        if (!claude.IsAvailable || string.IsNullOrWhiteSpace(name)) return null;

        var question =
            $"Existing tags ({existingTags.Count}):\n{string.Join("\n", existingTags.Select(t => "- " + t))}" +
            $"\n\nNew name typed by the admin: \"{name}\"";

        var json = await claude.AskForJsonAsync(Helper, Instructions, question, VerdictSchema, maxTokens: 300,
                                                cancellationToken);
        if (json is null) return null;

        try
        {
            var verdict = JsonSerializer.Deserialize<Verdict>(json, ClaudeMessages.Json);
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
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "AI tag check: the answer couldn't be read.");
            return null;
        }
    }

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
