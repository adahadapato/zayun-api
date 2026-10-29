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
