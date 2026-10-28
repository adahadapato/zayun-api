using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;

namespace Zayun.Application.Features.Presentations.Dtos;

/// <summary>A presentation as the website and the admin area show it.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Title">What was presented.</param>
/// <param name="EventName">Where and when.</param>
/// <param name="Year">The year.</param>
/// <param name="Type">Poster, oral, conference…</param>
/// <param name="Image">The poster, slide or certificate, if any.</param>
/// <param name="DisplayOrder">Its place in the list.</param>
/// <param name="IsPublished">False for a draft.</param>
public sealed record PresentationDto(
    Guid Id,
    string Title,
    string EventName,
    int Year,
    PresentationType Type,
    PresentationImageDto? Image,
    int DisplayOrder,
    bool IsPublished)
{
    /// <summary>Maps a presentation, with absolute picture links.</summary>
    /// <param name="p">The presentation.</param>
    /// <param name="storage">Turns stored paths into links.</param>
    /// <returns>The DTO.</returns>
    public static PresentationDto From(Presentation p, IFileStorage storage) =>
        new(p.Id, p.Title, p.EventName, p.Year, p.Type,
            p.Image is null
                ? null
                : new PresentationImageDto(
                    storage.GetPublicUrl(p.Image.Path),
                    p.Image.ThumbnailPath is null ? null : storage.GetPublicUrl(p.Image.ThumbnailPath),
                    p.Image.Dimensions.Width, p.Image.Dimensions.Height,
                    p.ImageAlt ?? p.Title),
            p.DisplayOrder, p.IsPublished);
}

/// <summary>A presentation's picture.</summary>
/// <param name="Url">The full-size picture.</param>
/// <param name="ThumbnailUrl">The small version, if any.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="AltText">Text for screen readers.</param>
public sealed record PresentationImageDto(string Url, string? ThumbnailUrl, int Width, int Height, string AltText);
