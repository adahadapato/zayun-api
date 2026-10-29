using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;

namespace Zayun.Application.Features.SiteImages.Dtos;

/// <summary>A named picture on the website (e.g. the owner's photo), with absolute links.</summary>
/// <param name="Slot">Where it's shown.</param>
/// <param name="Url">The full-size picture.</param>
/// <param name="ThumbnailUrl">The small version, if any.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="AltText">Text for screen readers.</param>
public sealed record SiteImageDto(SiteImageSlot Slot, string Url, string? ThumbnailUrl, int Width, int Height,
                                  string AltText)
{
    /// <summary>Maps a picture.</summary>
    public static SiteImageDto From(SiteImage s, IFileStorage storage) =>
        new(s.Slot, storage.GetPublicUrl(s.Image.Path),
            s.Image.ThumbnailPath is null ? null : storage.GetPublicUrl(s.Image.ThumbnailPath),
            s.Image.Dimensions.Width, s.Image.Dimensions.Height, s.AltText);
}
