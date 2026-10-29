using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Zayun.Infrastructure.Settings.Application;

namespace Zayun.Infrastructure.Settings.Storage;

/// <summary>
/// Creates and checks signed, expiring links to private documents:
/// <c>{ApiBaseUrl}/private-files/{path}?name={download name}&amp;expires={unix seconds}&amp;sig={HMAC-SHA256}</c>.
/// </summary>
/// <remarks>
/// <para>The signature covers the path, the download name AND the expiry time, so none can be changed without
/// breaking it. Comparison is constant-time, so the signature can't be guessed byte by byte.</para>
/// <para>Any failure (bad path, expired, wrong signature, missing file) just returns false, and the download
/// endpoint answers 404 without saying why.</para>
/// </remarks>
public sealed class PrivateFileLinks(
    IOptions<StorageSettings> storage,
    IOptions<ApplicationSettings> application,
    LocalStoragePaths paths,
    TimeProvider time)
{
    /// <summary>A link to a private file that stops working after the configured minutes.</summary>
    /// <param name="path">The file's storage path.</param>
    /// <param name="downloadName">The name the browser saves it as.</param>
    /// <returns>The signed link and its expiry (UTC).</returns>
    /// <exception cref="ArgumentException">An unsafe path or an empty name.</exception>
    public (Uri Url, DateTime ExpiresAt) Create(string path, string downloadName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadName);
        var relative = LocalStoragePaths.Normalise(path);
        var expiresAt = time.GetUtcNow().AddMinutes(storage.Value.Local.PrivateLinkMinutes);
        var expires = expiresAt.ToUnixTimeSeconds();
        var signature = Sign(relative, downloadName, expires);

        var url = new Uri($"{application.Value.ApiBaseUrl}{paths.PrivateRequestPath}/" +
                          $"{LocalStoragePaths.ToUrlPath(relative)}?name={Uri.EscapeDataString(downloadName)}" +
                          $"&expires={expires}&sig={signature}");
        return (url, expiresAt.UtcDateTime);
    }

    /// <summary>Checks a link and, if it's valid, gives the file's full path on disk.</summary>
    /// <param name="path">The path part of the link (already URL-decoded by routing).</param>
    /// <param name="downloadName">The link's <c>name</c> value.</param>
    /// <param name="expires">The link's <c>expires</c> value.</param>
    /// <param name="signature">The link's <c>sig</c> value.</param>
    /// <param name="fullPath">The file on disk, when the link is valid.</param>
    /// <returns>True only if the path is safe, the link hasn't expired, the signature matches and the file exists.</returns>
    public bool TryResolve(string path, string? downloadName, long expires, string? signature,
                           [NotNullWhen(true)] out string? fullPath)
    {
        fullPath = null;
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(downloadName)) return false;
        if (time.GetUtcNow().ToUnixTimeSeconds() > expires) return false;

        string relative;
        try { relative = LocalStoragePaths.Normalise(path); }
        catch (ArgumentException) { return false; }

        var expected = Encoding.ASCII.GetBytes(Sign(relative, downloadName, expires));
        var given = Encoding.ASCII.GetBytes(signature);
        if (!CryptographicOperations.FixedTimeEquals(expected, given)) return false;

        var candidate = paths.ResolvePrivateFullPath(relative);
        if (!File.Exists(candidate)) return false;

        fullPath = candidate;
        return true;
    }

    /// <summary>HMAC-SHA256 of "path\nname\nexpires" with the signing key, as URL-safe Base64.</summary>
    private string Sign(string relativePath, string downloadName, long expires)
    {
        var key = Encoding.UTF8.GetBytes(storage.Value.Local.SigningKey);
        var data = Encoding.UTF8.GetBytes($"{relativePath}\n{downloadName}\n{expires}");
        return Base64Url.EncodeToString(HMACSHA256.HashData(key, data));
    }
}
