using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Text;

namespace Zayun.Application.Common.Tags;

/// <summary>
/// Used when projects, experience areas and skill groups are saved: turns the typed tag / skill names into names from
/// the shared tag list (rules only, no AI), adding any new tags to the list in the same save.
/// </summary>
/// <remarks>
/// <para>Only the tags that could match the typed names are loaded, so this stays fast with thousands of tags.</para>
/// <para>The AI check runs earlier, while the admin types (see <c>CheckTagQuery</c>); saving stays fast and works even
/// when the AI can't be reached.</para>
/// </remarks>
public sealed class TagResolver(ITagRepository tags)
{
    /// <summary>Tags seen in this request (loaded or newly added), by Id.</summary>
    private readonly Dictionary<Guid, Tag> _known = [];

    /// <summary>The names as tag names: matched, deduplicated, in the original order. New tags are added.</summary>
    /// <param name="names">What was typed.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The tag names to store.</returns>
    public async Task<List<string>> ResolveAsync(IEnumerable<string>? names, CancellationToken cancellationToken)
    {
        var typed = (names ?? []).ToList();
        foreach (var tag in await tags.FindByKeysAsync(TagVocabulary.CandidateKeys(typed), cancellationToken))
            _known.TryAdd(tag.Id, tag);

        var vocabulary = new TagVocabulary(_known.Values);
        var resolved = vocabulary.ResolveAll(typed);
        foreach (var tag in vocabulary.Added)
        {
            _known[tag.Id] = tag;
            tags.Add(tag);
        }
        return resolved;
    }
}
