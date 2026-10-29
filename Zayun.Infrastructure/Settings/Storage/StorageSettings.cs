using System.ComponentModel.DataAnnotations;
using Zayun.Domain.Constants;

namespace Zayun.Infrastructure.Settings.Storage;

/// <summary>
/// The <c>"Storage"</c> section (keys in <see cref="AppSettings.Storage"/>): where pictures and private documents
/// are kept.
/// </summary>
public sealed class StorageSettings
{
    /// <summary>The section's name in appsettings.json.</summary>
    public const string SectionName = AppSettings.Storage.Section;

    /// <summary>The folders on the server's disk.</summary>
    [Required]
    public LocalStorageSettings Local { get; set; } = new();
}

/// <summary>The <c>"Storage:Local"</c> sub-section.</summary>
public sealed class LocalStorageSettings
{
    /// <summary>Shortest signing key accepted.</summary>
    public const int MinSigningKeyLength = 32;

    /// <summary>
    /// Folder for pictures, relative to the content root and inside the web root so it's served as static files
    /// (default <c>wwwroot/uploads</c> → <c>/uploads/…</c>).
    /// </summary>
    [Required]
    public string PublicRootPath { get; set; } = "wwwroot/uploads";

    /// <summary>
    /// Folder for private documents, relative to the content root and OUTSIDE the web root, so no address can reach
    /// it directly (default <c>App_Data/private-documents</c>).
    /// </summary>
    [Required]
    public string PrivateRootPath { get; set; } = "App_Data/private-documents";

    /// <summary>The URL path signed download links use.</summary>
    [Required, RegularExpression("^/[A-Za-z0-9_-]+(/[A-Za-z0-9_-]+)*$",
        ErrorMessage = "Storage:Local:PrivateDownloadPath must look like \"/private-files\" (leading slash, no trailing slash).")]
    public string PrivateDownloadPath { get; set; } = "/private-files";

    /// <summary>Signs download links (SECRET: User Secrets in development, the CI pipeline in production).</summary>
    [Required(ErrorMessage = "Storage:Local:SigningKey is empty. Set it in User Secrets (development) or as Storage__Local__SigningKey (production).")]
    [MinLength(MinSigningKeyLength, ErrorMessage = "Storage:Local:SigningKey must be at least 32 characters.")]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>How long a download link works.</summary>
    [Range(1, 60)]
    public int PrivateLinkMinutes { get; set; } = 5;
}
