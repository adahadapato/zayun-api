using Zayun.Domain.Entities;
using Zayun.Domain.Enums;

namespace Zayun.Application.Features.CareerRoles.Dtos;

/// <summary>A role as the website and the admin area show it.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Kind">Professional, research, teaching or other.</param>
/// <param name="Title">The job title.</param>
/// <param name="Organisation">Where.</param>
/// <param name="Location">The place; null when not shown.</param>
/// <param name="StartYear">The year it started.</param>
/// <param name="StartMonth">The month it started (1–12); null when only the year is shown.</param>
/// <param name="EndYear">The year it ended; null while current.</param>
/// <param name="EndMonth">The month it ended (1–12); null when only the year is shown or while current.</param>
/// <param name="IsCurrent">True while it's current ("Present").</param>
/// <param name="Summary">One line about the role; null for none.</param>
/// <param name="Highlights">Achievement bullets, in order.</param>
/// <param name="IsPublished">False for a draft.</param>
public sealed record CareerRoleDto(
    Guid Id,
    CareerRoleKind Kind,
    string Title,
    string Organisation,
    string? Location,
    int StartYear,
    int? StartMonth,
    int? EndYear,
    int? EndMonth,
    bool IsCurrent,
    string? Summary,
    IReadOnlyList<string> Highlights,
    bool IsPublished)
{
    /// <summary>Maps a role.</summary>
    /// <param name="r">The role.</param>
    /// <returns>The DTO.</returns>
    public static CareerRoleDto From(CareerRole r) =>
        new(r.Id, r.Kind, r.Title, r.Organisation, r.Location, r.StartYear, r.StartMonth, r.EndYear, r.EndMonth,
            r.IsCurrent, r.Summary, r.Highlights.ToList(), r.IsPublished);
}
