using Zayun.Domain.Entities;
using Zayun.Domain.Enums;

namespace Zayun.Application.Features.Publications.Dtos;

/// <summary>A publication as the website and the admin area show it.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Title">The paper's title.</param>
/// <param name="Authors">Everyone who wrote it.</param>
/// <param name="Venue">Where it appeared.</param>
/// <param name="Year">The year.</param>
/// <param name="Type">Conference paper, journal article…</param>
/// <param name="Award">An award, if any.</param>
/// <param name="Links">Where to read it, in order.</param>
/// <param name="DisplayOrder">Its place in the list.</param>
/// <param name="IsPublished">False for a draft.</param>
public sealed record PublicationDto(
    Guid Id,
    string Title,
    string Authors,
    string Venue,
    int Year,
    PublicationType Type,
    string? Award,
    IReadOnlyList<PublicationLinkDto> Links,
    int DisplayOrder,
    bool IsPublished)
{
    /// <summary>Maps a publication.</summary>
    /// <param name="p">The publication.</param>
    /// <returns>The DTO.</returns>
    public static PublicationDto From(Publication p) =>
        new(p.Id, p.Title, p.Authors, p.Venue, p.Year, p.Type, p.Award,
            p.Links.Select(l => new PublicationLinkDto(l.Label, l.Url, l.IsPrimary)).ToList(),
            p.DisplayOrder, p.IsPublished);
}

/// <summary>One "read it here" link.</summary>
/// <param name="Label">Button text.</param>
/// <param name="Url">The address.</param>
/// <param name="IsPrimary">Shown as the main button.</param>
public sealed record PublicationLinkDto(string Label, string Url, bool IsPrimary);
