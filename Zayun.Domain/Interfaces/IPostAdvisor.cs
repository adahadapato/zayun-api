namespace Zayun.Domain.Interfaces;

/// <summary>
/// The AI helper for social posts: drafts the words that go AROUND an item from the website (a quote now; projects,
/// awards and blog posts later) when the owner shares it on LinkedIn, WhatsApp or X. Implemented in Infrastructure
/// (Claude); when AI is switched off, <see cref="IsAvailable"/> is false.
/// </summary>
/// <remarks>
/// <para>Only drafts: the owner edits them and posts them himself. Nothing is saved or posted by the AI.</para>
/// <para>The AI never writes the item itself (the quote's words, who said it, the link): the caller adds those from
/// the saved fields, so a signed saying or a proverb can't be reworded.</para>
/// </remarks>
public interface IPostAdvisor
{
    /// <summary>True when the AI can be used (switched on, with a key).</summary>
    bool IsAvailable { get; }

    /// <summary>Drafts the intros and hashtags for one item.</summary>
    /// <param name="item">What is being shared, and the facts about it.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The drafts, or null when the AI couldn't be asked (no key, timeout, error): never throws for that.</returns>
    Task<PostDraft?> DraftAsync(PostDraftRequest item, CancellationToken cancellationToken = default);
}

/// <summary>What the AI is told about the item being shared.</summary>
/// <param name="What">What it is, in a few words, e.g. "a traditional proverb" or "the owner's own saying".</param>
/// <param name="Facts">The facts it may use, as label and value (e.g. "Who said it" → "Ada Lovelace"). Only these.</param>
/// <param name="Topics">Its themes or tags, to base the hashtags on.</param>
public sealed record PostDraftRequest(string What, IReadOnlyList<PostFact> Facts, IReadOnlyList<string> Topics);

/// <summary>One fact about the item.</summary>
/// <param name="Label">What it is, e.g. "Source".</param>
/// <param name="Value">The fact.</param>
public sealed record PostFact(string Label, string Value);

/// <summary>The AI's drafts.</summary>
/// <param name="LongIntro">One to three sentences for LinkedIn, before the item.</param>
/// <param name="ShortIntro">One short sentence for WhatsApp or X.</param>
/// <param name="Hashtags">Hashtags without the "#", e.g. "Biomimicry".</param>
public sealed record PostDraft(string LongIntro, string ShortIntro, IReadOnlyList<string> Hashtags);
