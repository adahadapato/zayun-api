using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Text;

namespace Zayun.Application.Common.Tags;

/// <summary>
/// Used when projects, experience areas and skill groups are saved: turns the typed tag / skill names into names from
/// the shared tag list (rules only, no AI), adding any new tags to the list in the same save.
/// </summary>
/// <remarks>The AI check runs earlier, while the admin types (see <c>CheckTagQuery</c>); saving stays fast and works
/// even when the AI can't be reached.</remarks>
public sealed class TagResolver(ITagRepository tags)
{
    private TagVocabulary? _vocabulary;

    /// <summary>The names as tag names: matched, deduplicated, in the original order. New tags are added.</summary>
    /// <param name="names">What was typed.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The tag names to store.</returns>
    public async Task<List<string>> ResolveAsync(IEnumerable<string>? names, CancellationToken cancellationToken)
    {
        _vocabulary ??= new TagVocabulary(await tags.GetAllAsync(cancellationToken));

        var before = _vocabulary.Added.Count;
        var resolved = _vocabulary.ResolveAll(names);
        foreach (var tag in _vocabulary.Added.Skip(before)) tags.Add(tag);
        return resolved;
    }
}
