using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Text;

namespace Zayun.Application.Common.Tags;

/// <summary>
/// Recounts how many projects, experience areas and skill groups use each tag, after anything that changes them (see
/// <see cref="RefreshesTagUsageAttribute"/>). Only the tags used now or before are loaded.
/// </summary>
public sealed class TagUsageRefresher(ITagRepository tags, IProjectRepository projects,
                                      IExperienceAreaRepository experience, ISkillGroupRepository skillGroups,
                                      IUnitOfWork unitOfWork)
{
    /// <summary>Recounts and saves.</summary>
    /// <param name="cancellationToken">Cancels the queries.</param>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        // One after the other: the repositories share one DbContext
        var projectTags = (await projects.GetListAsync(publishedOnly: false, cancellationToken))
                          .Select(p => (IReadOnlyList<string>)p.Tags).ToList();
        var experienceSkills = (await experience.GetListAsync(publishedOnly: false, cancellationToken))
                               .Select(e => (IReadOnlyList<string>)e.Skills).ToList();
        var groupSkills = (await skillGroups.GetListAsync(publishedOnly: false, cancellationToken))
                          .Select(g => (IReadOnlyList<string>)g.Skills).ToList();

        var usedNow = TagUsageCalculator.KeysIn([.. projectTags, .. experienceSkills, .. groupSkills]);
        var affected = (await tags.GetUsedAsync(cancellationToken))
                       .Concat(await tags.FindByKeysAsync(usedNow, cancellationToken))
                       .DistinctBy(t => t.Id)
                       .ToList();

        if (TagUsageCalculator.Apply(affected, projectTags, experienceSkills, groupSkills) > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Put this on a COMMAND that changes project tags, experience skills or skill-group skills. When it succeeds, the
/// tags' usage counts are recounted.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RefreshesTagUsageAttribute : Attribute;
