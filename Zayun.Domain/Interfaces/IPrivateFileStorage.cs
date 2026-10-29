namespace Zayun.Domain.Interfaces;

/// <summary>
/// Saves and deletes PRIVATE files (the owner's documents). Implemented in Infrastructure: a folder outside the web
/// root, so files can never be opened by guessing their address; they're only reachable through
/// <see cref="CreateDownloadLink"/>.
/// </summary>
public interface IPrivateFileStorage
{
    /// <summary>Saves the stream at the given path (e.g. "documents/{id}/{fileId}.pdf").</summary>
    /// <param name="content">The file.</param>
    /// <param name="path">Where to keep it, with forward slashes.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The path, as stored.</returns>
    Task<string> SaveAsync(Stream content, string path, CancellationToken cancellationToken = default);

    /// <summary>Deletes a file. Deleting a file that's already gone is not an error.</summary>
    /// <param name="path">The stored path.</param>
    /// <param name="cancellationToken">Cancels the delete.</param>
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>A signed link that downloads the file for a few minutes, then stops working.</summary>
    /// <param name="path">The stored path.</param>
    /// <param name="downloadName">The file name the browser saves it as.</param>
    /// <returns>The link and when it stops working (UTC).</returns>
    PrivateDownloadLink CreateDownloadLink(string path, string downloadName);
}

/// <summary>A signed, expiring download link.</summary>
/// <param name="Url">The link.</param>
/// <param name="ExpiresAt">When it stops working (UTC).</param>
public sealed record PrivateDownloadLink(Uri Url, DateTime ExpiresAt);
