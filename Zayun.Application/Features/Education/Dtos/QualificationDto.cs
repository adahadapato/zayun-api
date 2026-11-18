using Zayun.Domain.Entities;

namespace Zayun.Application.Features.Education.Dtos;

/// <summary>A qualification as the website and the admin area show it.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Degree">The degree or award, e.g. "M.Tech".</param>
/// <param name="Subject">The subject.</param>
/// <param name="Institution">Where.</param>
/// <param name="Location">The place; null when not shown.</param>
/// <param name="Year">The year completed; null while in progress.</param>
/// <param name="IsInProgress">True while it's in progress.</param>
/// <param name="Note">A grade or note; null for none.</param>
/// <param name="IsPublished">False for a draft.</param>
public sealed record QualificationDto(
    Guid Id,
    string Degree,
    string Subject,
    string Institution,
    string? Location,
    int? Year,
    bool IsInProgress,
    string? Note,
    bool IsPublished)
{
    /// <summary>Maps a qualification.</summary>
    /// <param name="q">The qualification.</param>
    /// <returns>The DTO.</returns>
    public static QualificationDto From(Qualification q) =>
        new(q.Id, q.Degree, q.Subject, q.Institution, q.Location, q.Year, q.IsInProgress, q.Note, q.IsPublished);
}
