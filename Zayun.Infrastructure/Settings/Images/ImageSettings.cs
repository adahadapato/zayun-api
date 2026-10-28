using System.ComponentModel.DataAnnotations;
using Zayun.Domain.Constants;

namespace Zayun.Infrastructure.Settings.Images;

/// <summary>
/// The <c>"Images"</c> section (keys in <see cref="AppSettings.Images"/>): upload limit and the WebP pipeline.
/// </summary>
public sealed class ImageSettings
{
    /// <summary>The section's name in appsettings.json.</summary>
    public const string SectionName = AppSettings.Images.Section;

    /// <summary>Largest upload accepted (default 15 MB: posters and screenshots can be big).</summary>
    [Range(1, 50 * 1024 * 1024)]
    public long MaxUploadBytes { get; set; } = 15 * 1024 * 1024;

    /// <summary>Longest side of the stored picture; bigger uploads are shrunk (never enlarged).</summary>
    [Range(400, 6000)]
    public int MaxSide { get; set; } = 1920;

    /// <summary>Longest side of the thumbnail used on cards.</summary>
    [Range(100, 2000)]
    public int ThumbnailMaxSide { get; set; } = 640;

    /// <summary>WebP quality, 1 to 100.</summary>
    [Range(1, 100)]
    public int WebpQuality { get; set; } = 82;
}
