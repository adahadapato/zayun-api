using Zayun.Domain.Entities;

namespace Zayun.Domain.Shared.Text;

/// <summary>
/// Turns typed names into tags from the shared list, adding new ones when needed. Used when content is saved and when
/// the starter content is first loaded. No AI here: only the rules in <see cref="TagNames"/> and the existing tags.
/// </summary>
/// <remarks>
/// For each name, in order:
/// <list type="number">
///   <item><description>an existing tag with the same key or alias ("rest api" → "REST API");</description></item>
///   <item><description>a built-in synonym ("RESTful APIs" → "REST API", "MS SQL" → "SQL Server"), found or added;</description></item>
///   <item><description>an existing tag in the singular / plural ("Transformer" → "Transformers");</description></item>
///   <item><description>otherwise a new tag, spelled as typed (cleaned).</description></item>
/// </list>
/// </remarks>
public sealed class TagVocabulary
{
    private readonly List<Tag> _tags;
    private readonly List<Tag> _added = [];

    /// <summary>Starts from the tags already stored.</summary>
    /// <param name="existing">The stored tags that could match (see <see cref="CandidateKeys"/>), or all of them.</param>
    public TagVocabulary(IEnumerable<Tag> existing) => _tags = [.. existing];

    /// <summary>
    /// Every key <see cref="Find"/> may look up for these names (the key, its built-in synonym and its singular /
    /// plural), so only the tags that could match need loading.
    /// </summary>
    /// <param name="names">What was typed.</param>
    /// <returns>The keys.</returns>
    public static HashSet<string> CandidateKeys(IEnumerable<string?> names)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            var key = TagNames.Key(name);
            if (key.Length == 0) continue;
            keys.Add(key);
            if (TagNames.OtherNumber(key) is { } other) keys.Add(other);
            if (TagNames.BuiltIn(name) is { } builtIn)
            {
                var builtInKey = TagNames.Key(builtIn);
                keys.Add(builtInKey);
                if (TagNames.OtherNumber(builtInKey) is { } otherBuiltIn) keys.Add(otherBuiltIn);
            }
        }
        return keys;
    }

    /// <summary>Tags created by <see cref="Resolve(string)"/> (add these to the database).</summary>
    public IReadOnlyList<Tag> Added => _added;

    /// <summary>The tag for one typed name, found or created.</summary>
    /// <param name="name">What was typed.</param>
    /// <returns>The tag, or null for an empty name.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A name longer than <see cref="TagNames.MaxLength"/>.</exception>
    public Tag? Resolve(string name)
    {
        var clean = TagNames.Clean(name);
        if (clean.Length == 0) return null;
        return Find(clean) ?? Add(TagNames.BuiltIn(clean) ?? clean);
    }

    /// <summary>Every name in a list, as tag names: matched, deduplicated, in the original order.</summary>
    /// <param name="names">What was typed.</param>
    /// <returns>The tag names.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A name longer than <see cref="TagNames.MaxLength"/>.</exception>
    public List<string> ResolveAll(IEnumerable<string>? names)
    {
        var result = new List<string>();
        foreach (var name in names ?? [])
        {
            var tag = Resolve(name);
            if (tag is not null && !result.Contains(tag.Name)) result.Add(tag.Name);
        }
        return result;
    }

    /// <summary>An existing tag for this name, without creating one (for the live check while typing).</summary>
    /// <param name="name">What was typed.</param>
    /// <returns>The matching tag, or null.</returns>
    public Tag? Find(string name)
    {
        var key = TagNames.Key(name);
        if (key.Length == 0) return null;

        var match = _tags.FirstOrDefault(t => t.Matches(key));
        if (match is not null) return match;

        var builtIn = TagNames.BuiltIn(name);
        if (builtIn is not null)
        {
            var builtInKey = TagNames.Key(builtIn);
            match = _tags.FirstOrDefault(t => t.Matches(builtInKey));
            if (match is not null) return match;
        }

        var otherNumber = TagNames.OtherNumber(key);
        return otherNumber is null ? null : _tags.FirstOrDefault(t => t.Matches(otherNumber));
    }

    private Tag Add(string name)
    {
        var tag = Tag.Create(name);
        _tags.Add(tag);
        _added.Add(tag);
        return tag;
    }
}
