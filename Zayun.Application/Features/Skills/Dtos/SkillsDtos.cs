using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Skills.Dtos;

/// <summary>One core expertise item.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Name">The expertise.</param>
/// <param name="DisplayOrder">Its place in the list.</param>
/// <param name="IsPublished">False for a draft.</param>
public sealed record CoreExpertiseDto(Guid Id, string Name, int DisplayOrder, bool IsPublished)
{
    /// <summary>Maps a core expertise item.</summary>
    /// <param name="c">The item.</param>
    /// <returns>The DTO.</returns>
    public static CoreExpertiseDto From(CoreExpertise c) => new(c.Id, c.Name, c.DisplayOrder, c.IsPublished);
}

/// <summary>Everything the Skills page shows: the skill groups and the core expertise list.</summary>
/// <param name="Groups">The skill groups, in order.</param>
/// <param name="CoreExpertise">The core expertise list, in order.</param>
public sealed record SkillsPageDto(IReadOnlyList<SkillGroupDto> Groups, IReadOnlyList<CoreExpertiseDto> CoreExpertise);
