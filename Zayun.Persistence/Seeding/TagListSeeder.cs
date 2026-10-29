using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zayun.Domain.Shared.Text;

namespace Zayun.Persistence.Seeding;

/// <summary>
/// Fills the shared tag list the first time (while the <c>Tags</c> table is empty) from the names already used by
/// projects, experience areas and skill groups, and tidies those names on the way: "REST APIs" becomes "REST API",
/// "EF Core" becomes "Entity Framework Core", "MS SQL" becomes "SQL Server" (the built-in rules in
/// <see cref="TagNames"/>).
/// </summary>
/// <remarks>Runs after the starter content, so a new database and an existing one both end up with a full list. After
/// that, tags are added when content is saved, or in Admin → Tags.</remarks>
internal static class TagListSeeder
{
    /// <summary>Builds the tag list when it's empty.</summary>
    /// <param name="context">The database.</param>
    /// <param name="logger">Start-up log.</param>
    /// <param name="cancellationToken">Stops seeding.</param>
    public static async Task SeedAsync(ZayunDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        await BuildAsync(context, logger, cancellationToken);
        await RepairAsync(context, logger, cancellationToken);
        await RecountAsync(context, logger, cancellationToken);
    }

    /// <summary>
    /// Fixes tags whose search keys don't contain their own key: tags stored before the <c>SearchKeys</c> column was
    /// added got an empty value, so lookups and the usage recount couldn't find them.
    /// </summary>
    private static async Task RepairAsync(ZayunDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        var repaired = 0;
        while (true)
        {
            var broken = await context.Tags
                .Where(t => !t.SearchKeys.Contains("|" + t.NormalizedName + "|"))
                .Take(500)
                .ToListAsync(cancellationToken);
            if (broken.Count == 0) break;

            foreach (var tag in broken)
                if (tag.RepairSearchKeys()) repaired++;
            await context.SaveChangesAsync(cancellationToken);

            if (broken.Count < 500) break;
        }

        if (repaired > 0) logger.LogInformation("Repaired the search keys of {Count} tags", repaired);
    }

    /// <summary>
    /// Recounts where every tag is used, on every start, so the numbers are right even after a change that failed to
    /// update them. Only the tags used now or before are loaded.
    /// </summary>
    private static async Task RecountAsync(ZayunDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        var projectTags = (await context.Projects.AsNoTracking().Select(p => p.Tags).ToListAsync(cancellationToken))
                          .Cast<IReadOnlyList<string>>().ToList();
        var experienceSkills = (await context.ExperienceAreas.AsNoTracking().Select(e => e.Skills)
                                             .ToListAsync(cancellationToken)).Cast<IReadOnlyList<string>>().ToList();
        var groupSkills = (await context.SkillGroups.AsNoTracking().Select(g => g.Skills)
                                        .ToListAsync(cancellationToken)).Cast<IReadOnlyList<string>>().ToList();

        var usedKeys = TagUsageCalculator.KeysIn([.. projectTags, .. experienceSkills, .. groupSkills]);
        var affected = await context.Tags
            .Where(t => t.ProjectCount + t.ExperienceAreaCount + t.SkillGroupCount > 0)
            .ToListAsync(cancellationToken);
        foreach (var key in usedKeys)
        {
            var token = Domain.Entities.Tag.SearchToken(key);
            affected.AddRange(await context.Tags.Where(t => t.NormalizedName == key || t.SearchKeys.Contains(token))
                                                .ToListAsync(cancellationToken));
        }

        var changed = TagUsageCalculator.Apply(affected.DistinctBy(t => t.Id), projectTags, experienceSkills, groupSkills);
        if (changed == 0) return;

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Recounted tag usage: {Count} tags updated", changed);
    }

    /// <summary>Builds the tag list from the content, once (while the Tags table is empty).</summary>
    private static async Task BuildAsync(ZayunDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        if (await context.Tags.IgnoreQueryFilters().AnyAsync(cancellationToken)) return;

        var vocabulary = new TagVocabulary([]);
        var tidied = 0;

        foreach (var project in await context.Projects.ToListAsync(cancellationToken))
            foreach (var name in project.Tags.ToList())
                if (Tidy(vocabulary, name) is { } tag && project.ReplaceTag(name, tag)) tidied++;

        foreach (var area in await context.ExperienceAreas.ToListAsync(cancellationToken))
            foreach (var name in area.Skills.ToList())
                if (Tidy(vocabulary, name) is { } tag && area.ReplaceSkill(name, tag)) tidied++;

        foreach (var group in await context.SkillGroups.ToListAsync(cancellationToken))
            foreach (var name in group.Skills.ToList())
                if (Tidy(vocabulary, name) is { } tag && group.ReplaceSkill(name, tag)) tidied++;

        if (vocabulary.Added.Count == 0) return;

        foreach (var tag in vocabulary.Added) context.Tags.Add(tag);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Built the tag list: {Count} tags, {Tidied} names tidied", vocabulary.Added.Count, tidied);
    }

    /// <summary>The tag name for a stored name, or null when it's already right.</summary>
    private static string? Tidy(TagVocabulary vocabulary, string name)
    {
        var tag = vocabulary.Resolve(name);
        return tag is null || tag.Name == name ? null : tag.Name;
    }
}
