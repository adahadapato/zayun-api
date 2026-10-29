using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Tags.Dtos;

/// <summary>A tag in the shared list, with where it's used.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Name">The name as shown.</param>
/// <param name="NormalizedName">The UPPERCASE key used to spot duplicates.</param>
/// <param name="Aliases">Other keys that mean this tag (old names).</param>
/// <param name="Projects">Projects that use it.</param>
/// <param name="ExperienceAreas">Experience areas that use it.</param>
/// <param name="SkillGroups">Skill groups that use it.</param>
public sealed record TagDto(
    Guid Id,
    string Name,
    string NormalizedName,
    IReadOnlyList<string> Aliases,
    int Projects,
    int ExperienceAreas,
    int SkillGroups)
{
    /// <summary>Everything that uses it.</summary>
    public int UsedBy => Projects + ExperienceAreas + SkillGroups;

    /// <summary>Maps a tag with its usage counts.</summary>
    public static TagDto From(Tag tag, TagUsage usage) =>
        new(tag.Id, tag.Name, tag.NormalizedName, tag.Aliases, usage.Projects, usage.ExperienceAreas,
            usage.SkillGroups);
}

/// <summary>How many items use a tag.</summary>
/// <param name="Projects">Projects.</param>
/// <param name="ExperienceAreas">Experience areas.</param>
/// <param name="SkillGroups">Skill groups.</param>
public sealed record TagUsage(int Projects, int ExperienceAreas, int SkillGroups)
{
    /// <summary>Not used anywhere.</summary>
    public static readonly TagUsage None = new(0, 0, 0);

    /// <summary>Everything that uses it.</summary>
    public int Total => Projects + ExperienceAreas + SkillGroups;
}

/// <summary>The result of checking a typed tag name (while typing, before it's added).</summary>
/// <param name="Status">Existing tag, new tag, or doubtful (the AI thinks it isn't a skill).</param>
/// <param name="Name">The name to use: the existing tag's name, or the proper spelling of a new one.</param>
/// <param name="TagId">The existing tag's Id, when <paramref name="Status"/> is Existing.</param>
/// <param name="Message">A short line for the admin, e.g. "Means the same as REST API."</param>
/// <param name="CheckedByAi">True when the AI was asked.</param>
public sealed record TagCheckDto(TagCheckStatus Status, string Name, Guid? TagId, string Message, bool CheckedByAi);

/// <summary>What a typed tag name turned out to be.</summary>
public enum TagCheckStatus
{
    /// <summary>Already in the list (maybe under another spelling).</summary>
    Existing = 1,

    /// <summary>Not in the list yet; it will be added when the item is saved.</summary>
    New = 2,

    /// <summary>The AI doesn't think it's a skill or technology; ask before adding.</summary>
    Doubtful = 3
}
