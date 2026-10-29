using Microsoft.Extensions.Logging;
using Zayun.Domain.Interfaces;

namespace Zayun.Infrastructure.Settings.Storage;

/// <summary>
/// <see cref="IPrivateFileStorage"/> on the server's disk: files go under the private folder (outside wwwroot) and
/// can only be downloaded through <see cref="PrivateFileLinks"/>.
/// </summary>
/// <remarks>The private folder sits in App_Data, which the deployment keeps and IIS never serves.</remarks>
internal sealed class LocalPrivateFileStorage(
    LocalStoragePaths paths,
    PrivateFileLinks links,
    ILogger<LocalPrivateFileStorage> logger) : IPrivateFileStorage
{
    private const int BufferSize = 81_920;

    /// <inheritdoc />
    public async Task<string> SaveAsync(Stream content, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var relative = LocalStoragePaths.Normalise(path);
        var fullPath = paths.ResolvePrivateFullPath(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        if (content.CanSeek) content.Position = 0;

        try
        {
            await using var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None,
                                                  BufferSize, useAsync: true);
            await content.CopyToAsync(file, BufferSize, cancellationToken);
        }
        catch
        {
            TryDelete(fullPath);
            throw;
        }

        logger.LogInformation("Stored private file {Path}", relative);
        return relative;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = paths.ResolvePrivateFullPath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            logger.LogInformation("Deleted private file {Path}", path);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public PrivateDownloadLink CreateDownloadLink(string path, string downloadName)
    {
        var (url, expiresAt) = links.Create(path, downloadName);
        return new PrivateDownloadLink(url, expiresAt);
    }

    private void TryDelete(string fullPath)
    {
        try { if (File.Exists(fullPath)) File.Delete(fullPath); }
        catch (Exception ex) { logger.LogWarning(ex, "Couldn't remove half-written file {FullPath}", fullPath); }
    }
}
