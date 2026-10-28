using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Zayun.Infrastructure.Settings.Storage;

/// <summary>
/// Turns storage paths ("projects/{id}/{imageId}.webp") into folders on disk and URL paths, and refuses anything that
/// would escape the uploads folder.
/// </summary>
/// <remarks>Singleton: works the folders out once from the settings and the web root.</remarks>
public sealed class LocalStoragePaths
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>Works out the folders and creates the uploads folder if it's missing.</summary>
    /// <param name="options">The storage settings.</param>
    /// <param name="environment">Where the app and its web root are.</param>
    /// <exception cref="InvalidOperationException">No web root, or the uploads folder isn't inside it.</exception>
    public LocalStoragePaths(IOptions<StorageSettings> options, IWebHostEnvironment environment)
    {
        var webRoot = environment.WebRootPath
            ?? throw new InvalidOperationException(
                "The API has no web root folder. Create 'wwwroot' in Zayun.Presentation (it can't be added while running).");
        webRoot = Path.GetFullPath(webRoot);

        PublicRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.Local.PublicRootPath));
        if (!IsInside(PublicRoot, webRoot))
            throw new InvalidOperationException($"Storage:Local:PublicRootPath must be a folder inside the web root ({webRoot}).");

        PublicRequestPath = "/" + Path.GetRelativePath(webRoot, PublicRoot).Replace(Path.DirectorySeparatorChar, '/');
        Directory.CreateDirectory(PublicRoot);
    }

    /// <summary>The uploads folder on disk.</summary>
    public string PublicRoot { get; }

    /// <summary>The URL path it's served at, e.g. "/uploads".</summary>
    public string PublicRequestPath { get; }

    /// <summary>Cleans a storage path: forward slashes, no leading slash, no "." or ".." parts.</summary>
    /// <param name="path">The path.</param>
    /// <returns>The clean path.</returns>
    /// <exception cref="ArgumentException">An empty, rooted or escaping path.</exception>
    public static string Normalise(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var clean = path.Trim().Replace('\\', '/').TrimStart('/');
        var segments = clean.Split('/');

        if (clean.Length == 0 || Path.IsPathRooted(clean) ||
            segments.Any(s => s.Length == 0 || s == "." || s == ".." || s.Contains(':')))
            throw new ArgumentException($"'{path}' isn't a valid storage path.", nameof(path));

        return clean;
    }

    /// <summary>The full path on disk for a storage path.</summary>
    /// <param name="path">The storage path.</param>
    /// <returns>The full path, always inside <see cref="PublicRoot"/>.</returns>
    /// <exception cref="ArgumentException">A path that would leave the uploads folder.</exception>
    public string ResolveFullPath(string path)
    {
        var relative = Normalise(path);
        var full = Path.GetFullPath(Path.Combine(PublicRoot, relative.Replace('/', Path.DirectorySeparatorChar)));

        // Belt and braces: even after Normalise, never allow a path outside the root
        if (!IsInside(full, PublicRoot))
            throw new ArgumentException($"'{path}' points outside the storage folder.", nameof(path));

        return full;
    }

    /// <summary>Escapes each part of a storage path for use in a URL.</summary>
    /// <param name="relativePath">A clean storage path.</param>
    /// <returns>The URL path.</returns>
    public static string ToUrlPath(string relativePath) =>
        string.Join('/', relativePath.Split('/').Select(Uri.EscapeDataString));

    private static bool IsInside(string folder, string parent) =>
        folder.StartsWith(parent.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, PathComparison);
}
