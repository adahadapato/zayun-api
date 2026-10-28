using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Skills.Dtos;

/// <summary>A skill group as the website and the admin area show it.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Title">The group's name.</param>
/// <param name="Description">What the group covers.</param>
/// <param name="Skills">The skills.</param>
/// <param name="DisplayOrder">Its place in the list.</param>
/// <param name="IsPublished">False for a draft.</param>
public sealed record SkillGroupDto(
    Guid Id,
    string Title,
    string Description,
    IReadOnlyList<string> Skills,
    int DisplayOrder,
    bool IsPublished)
{
    /// <summary>Maps a skill group.</summary>
    /// <param name="e">The group.</param>
    /// <returns>The DTO.</returns>
    public static SkillGroupDto From(SkillGroup g) =>
        new(g.Id, g.Title, g.Description, g.Skills.ToList(), g.DisplayOrder, g.IsPublished);
}
