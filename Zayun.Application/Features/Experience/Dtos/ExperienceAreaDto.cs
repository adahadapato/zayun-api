using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Experience.Dtos;

/// <summary>An experience area as the website and the admin area show it.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Title">The area's name.</param>
/// <param name="Description">What it involved.</param>
/// <param name="Skills">Skills used.</param>
/// <param name="DisplayOrder">Its place in the list.</param>
/// <param name="IsPublished">False for a draft.</param>
public sealed record ExperienceAreaDto(
    Guid Id,
    string Title,
    string Description,
    IReadOnlyList<string> Skills,
    int DisplayOrder,
    bool IsPublished)
{
    /// <summary>Maps an experience area.</summary>
    /// <param name="e">The area.</param>
    /// <returns>The DTO.</returns>
    public static ExperienceAreaDto From(ExperienceArea e) =>
        new(e.Id, e.Title, e.Description, e.Skills.ToList(), e.DisplayOrder, e.IsPublished);
}
