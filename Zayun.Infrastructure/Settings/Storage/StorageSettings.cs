using System.ComponentModel.DataAnnotations;
using Zayun.Domain.Constants;

namespace Zayun.Infrastructure.Settings.Storage;

/// <summary>
/// The <c>"Storage"</c> section (keys in <see cref="AppSettings.Storage"/>): where uploaded pictures are kept.
/// </summary>
public sealed class StorageSettings
{
    /// <summary>The section's name in appsettings.json.</summary>
    public const string SectionName = AppSettings.Storage.Section;

    /// <summary>The folder under the web root.</summary>
    [Required]
    public LocalStorageSettings Local { get; set; } = new();
}

/// <summary>The <c>"Storage:Local"</c> sub-section.</summary>
public sealed class LocalStorageSettings
{
    /// <summary>
    /// Folder for uploaded pictures, relative to the app's content root, and inside the web root so it's served
    /// as static files (default <c>wwwroot/uploads</c> → <c>/uploads/…</c>).
    /// </summary>
    [Required]
    public string PublicRootPath { get; set; } = "wwwroot/uploads";
}
