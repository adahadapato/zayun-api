using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Application.Features.Awards.Dtos;

/// <summary>An award as the website and the admin area show it.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Title">What it's called.</param>
/// <param name="AwardedBy">Who gave it, or the event.</param>
/// <param name="Year">The year.</param>
/// <param name="Description">What it was for.</param>
/// <param name="Url">Where to read more, if anywhere.</param>
/// <param name="IsFeatured">Also shown on the About and Experience pages.</param>
/// <param name="Image">The certificate or photo, if any.</param>
/// <param name="DisplayOrder">Its place in the list.</param>
/// <param name="IsPublished">False for a draft.</param>
public sealed record AwardDto(
    Guid Id,
    string Title,
    string AwardedBy,
    int Year,
    string Description,
    string? Url,
    bool IsFeatured,
    AwardImageDto? Image,
    int DisplayOrder,
    bool IsPublished)
{
    /// <summary>Maps an award, with absolute picture links.</summary>
    /// <param name="a">The award.</param>
    /// <param name="storage">Turns stored paths into links.</param>
    /// <returns>The DTO.</returns>
    public static AwardDto From(Award a, IFileStorage storage) =>
        new(a.Id, a.Title, a.AwardedBy, a.Year, a.Description, a.Url, a.IsFeatured,
            a.Image is null
                ? null
                : new AwardImageDto(
                    storage.GetPublicUrl(a.Image.Path),
                    a.Image.ThumbnailPath is null ? null : storage.GetPublicUrl(a.Image.ThumbnailPath),
                    a.Image.Dimensions.Width, a.Image.Dimensions.Height,
                    a.ImageAlt ?? a.Title),
            a.DisplayOrder, a.IsPublished);
}

/// <summary>An award's picture.</summary>
/// <param name="Url">The full-size picture.</param>
/// <param name="ThumbnailUrl">The small version, if any.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="AltText">Text for screen readers.</param>
public sealed record AwardImageDto(string Url, string? ThumbnailUrl, int Width, int Height, string AltText);
