using Zayun.Domain.Entities;

namespace Zayun.Domain.Shared.Text;

/// <summary>
/// Works out how many projects, experience areas and skill groups use each tag, and stores the numbers on the tags.
/// An item counts once per tag, however the name was written ("REST APIs" counts for "REST API" through its key or
/// alias).
/// </summary>
public static class TagUsageCalculator
{
    /// <summary>The keys used by the content, so only those tags need loading.</summary>
    /// <param name="lists">Every tag / skill list.</param>
    /// <returns>Every key in them.</returns>
    public static HashSet<string> KeysIn(IEnumerable<IReadOnlyList<string>> lists) =>
        lists.SelectMany(l => l).Select(TagNames.Key).Where(k => k.Length > 0).ToHashSet(StringComparer.Ordinal);

    /// <summary>Sets the usage on each tag.</summary>
    /// <param name="tags">The tags to update: every tag used now or before.</param>
    /// <param name="projectTags">Each project's tags.</param>
    /// <param name="experienceSkills">Each experience area's skills.</param>
    /// <param name="skillGroupSkills">Each skill group's skills.</param>
    /// <returns>How many tags changed.</returns>
    public static int Apply(IEnumerable<Tag> tags,
                            IEnumerable<IReadOnlyList<string>> projectTags,
                            IEnumerable<IReadOnlyList<string>> experienceSkills,
                            IEnumerable<IReadOnlyList<string>> skillGroupSkills)
    {
        var projects = KeySets(projectTags);
        var experience = KeySets(experienceSkills);
        var groups = KeySets(skillGroupSkills);

        var changed = 0;
        foreach (var tag in tags)
        {
            if (tag.SetUsage(Count(projects, tag), Count(experience, tag), Count(groups, tag))) changed++;
        }
        return changed;
    }

    private static List<HashSet<string>> KeySets(IEnumerable<IReadOnlyList<string>> lists) =>
        lists.Select(l => l.Select(TagNames.Key).ToHashSet(StringComparer.Ordinal)).ToList();

    private static int Count(List<HashSet<string>> items, Tag tag) =>
        items.Count(keys => keys.Contains(tag.NormalizedName) || tag.Aliases.Any(keys.Contains));
}
