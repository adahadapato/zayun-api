using Zayun.Application.Features.Tags.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Text;

namespace Zayun.Application.Features.Tags.Queries;

/// <summary>Counts where each tag is used: project tags, experience skills and skill-group skills (drafts included).</summary>
public sealed class TagUsageCounter(IProjectRepository projects, IExperienceAreaRepository experience,
                                    ISkillGroupRepository skillGroups)
{
    /// <summary>Usage by tag key.</summary>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>A lookup from <see cref="Tag.NormalizedName"/> to its usage.</returns>
    public async Task<Func<Tag, TagUsage>> CountAsync(CancellationToken cancellationToken)
    {
        // One after the other: the repositories share one DbContext
        var projectKeys = Keys((await projects.GetListAsync(publishedOnly: false, cancellationToken)).Select(p => p.Tags));
        var experienceKeys = Keys((await experience.GetListAsync(publishedOnly: false, cancellationToken)).Select(e => e.Skills));
        var groupKeys = Keys((await skillGroups.GetListAsync(publishedOnly: false, cancellationToken)).Select(g => g.Skills));

        return tag => new TagUsage(Count(projectKeys, tag), Count(experienceKeys, tag), Count(groupKeys, tag));
    }

    /// <summary>Each item's set of keys.</summary>
    private static List<HashSet<string>> Keys(IEnumerable<List<string>> lists) =>
        lists.Select(list => list.Select(TagNames.Key).ToHashSet()).ToList();

    private static int Count(List<HashSet<string>> items, Tag tag) => items.Count(keys => keys.Contains(tag.NormalizedName));
}
