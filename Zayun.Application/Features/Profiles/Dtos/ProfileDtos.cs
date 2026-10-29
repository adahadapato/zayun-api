using Zayun.Domain.Entities;
using Zayun.Domain.Enums;

namespace Zayun.Application.Features.Profiles.Dtos;

/// <summary>An online profile as the website and the admin area show it.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Platform">Which site (picks the icon).</param>
/// <param name="Name">The name shown.</param>
/// <param name="Url">The profile's address.</param>
/// <param name="Label">An optional short line, e.g. the ORCID iD.</param>
/// <param name="ShowInFooter">Shown in the footer.</param>
/// <param name="ShowOnContact">Shown on the Contact page.</param>
/// <param name="ShowOnResearch">Shown on the Research page.</param>
/// <param name="DisplayOrder">Its place in the list.</param>
/// <param name="IsPublished">False for a draft.</param>
public sealed record ProfileDto(
    Guid Id,
    ProfilePlatform Platform,
    string Name,
    string Url,
    string? Label,
    bool ShowInFooter,
    bool ShowOnContact,
    bool ShowOnResearch,
    int DisplayOrder,
    bool IsPublished)
{
    /// <summary>Maps a profile.</summary>
    public static ProfileDto From(Profile p) =>
        new(p.Id, p.Platform, p.Name, p.Url, p.Label, p.ShowInFooter, p.ShowOnContact, p.ShowOnResearch,
            p.DisplayOrder, p.IsPublished);
}
