namespace Zayun.Domain.Interfaces;

/// <summary>
/// Saves and deletes uploaded files. Implemented in Infrastructure (a folder under the web root, served at
/// <c>/uploads</c>). The database only ever stores the returned path.
/// </summary>
public interface IFileStorage
{
    /// <summary>Saves the stream at the given path (e.g. "projects/{projectId}/{imageId}.webp").</summary>
    /// <param name="content">The file.</param>
    /// <param name="path">Where to keep it, with forward slashes.</param>
    /// <param name="contentType">e.g. "image/webp".</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The path, as stored.</returns>
    Task<string> SaveAsync(Stream content, string path, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Deletes a file. Deleting a file that's already gone is not an error.</summary>
    /// <param name="path">The stored path.</param>
    /// <param name="cancellationToken">Cancels the delete.</param>
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>The permanent, absolute URL the website loads the file from.</summary>
    /// <param name="path">The stored path.</param>
    string GetPublicUrl(string path);
}
