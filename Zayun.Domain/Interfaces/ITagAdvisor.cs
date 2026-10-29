namespace Zayun.Domain.Interfaces;

/// <summary>
/// The AI tag check: asks a language model whether a newly typed tag means the same as an existing one ("RESTful
/// services" = "REST API"), how it is normally spelled ("pytroch" → "PyTorch"), or isn't a skill at all. Implemented in
/// Infrastructure (Claude); when AI is switched off, <see cref="IsAvailable"/> is false and only the built-in rules are
/// used.
/// </summary>
public interface ITagAdvisor
{
    /// <summary>True when the AI check can be used (switched on, with a key).</summary>
    bool IsAvailable { get; }

    /// <summary>Checks one new name against the existing tags.</summary>
    /// <param name="name">The cleaned name that was typed.</param>
    /// <param name="existingTags">Every existing tag name.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The advice, or null when the AI couldn't be asked (no key, timeout, error): never throws for that.</returns>
    Task<TagAdvice?> AdviseAsync(string name, IReadOnlyList<string> existingTags,
                                 CancellationToken cancellationToken = default);
}

/// <summary>What the AI said about a new tag name.</summary>
/// <param name="Verdict">Same as an existing tag, a new tag, or not a tag.</param>
/// <param name="Name">The existing tag it matches, or the usual spelling for a new one.</param>
/// <param name="Reason">A short explanation, shown to the admin.</param>
public sealed record TagAdvice(TagAdviceVerdict Verdict, string Name, string? Reason);

/// <summary>The AI's verdict on a new tag name.</summary>
public enum TagAdviceVerdict
{
    /// <summary>Means the same as an existing tag (named in <see cref="TagAdvice.Name"/>).</summary>
    SameAsExisting = 1,

    /// <summary>A real skill, technology or topic not in the list yet.</summary>
    NewTag = 2,

    /// <summary>Not a skill or technology (a typo nobody could read, a sentence, something unrelated).</summary>
    NotATag = 3
}
