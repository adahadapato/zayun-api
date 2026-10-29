using Zayun.Domain.Entities;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Tags.Dtos;

/// <summary>A tag in the shared list, with where it's used.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Name">The name as shown.</param>
/// <param name="NormalizedName">The UPPERCASE key used to spot duplicates.</param>
/// <param name="Aliases">Other keys that mean this tag (old names).</param>
/// <param name="Projects">Projects that use it.</param>
/// <param name="ExperienceAreas">Experience areas that use it.</param>
/// <param name="SkillGroups">Skill groups that use it.</param>
/// <param name="UsedBy">Everything that uses it.</param>
public sealed record TagDto(
    Guid Id,
    string Name,
    string NormalizedName,
    IReadOnlyList<string> Aliases,
    int Projects,
    int ExperienceAreas,
    int SkillGroups,
    int UsedBy)
{
    /// <summary>Maps a tag.</summary>
    public static TagDto From(Tag tag) =>
        new(tag.Id, tag.Name, tag.NormalizedName, tag.Aliases, tag.ProjectCount, tag.ExperienceAreaCount,
            tag.SkillGroupCount, tag.UsageCount);
}

/// <summary>One page of the admin tag list, plus the totals for its filter tabs.</summary>
/// <param name="Tags">The page.</param>
/// <param name="Total">Every tag.</param>
/// <param name="Used">Tags something uses.</param>
/// <param name="Unused">Tags nothing uses.</param>
public sealed record TagListDto(PagedResult<TagDto> Tags, int Total, int Used, int Unused);

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
