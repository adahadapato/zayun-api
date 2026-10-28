using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zayun.Domain.Interfaces;
using Zayun.Infrastructure.Settings.Application;

namespace Zayun.Infrastructure.Settings.Storage;

/// <summary>
/// <see cref="IFileStorage"/> on the server's disk: files go under the uploads folder (inside wwwroot) and are served
/// as static files at <c>{ApiBaseUrl}/uploads/…</c>.
/// </summary>
/// <remarks>The uploads folder holds runtime data: the deployment must keep it (the pipeline does).</remarks>
internal sealed class LocalFileStorage(
    LocalStoragePaths paths,
    IOptions<ApplicationSettings> application,
    ILogger<LocalFileStorage> logger) : IFileStorage
{
    private const int BufferSize = 81_920;

    /// <inheritdoc />
    public async Task<string> SaveAsync(Stream content, string path, string contentType,
                                        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);   // the file extension carries the type on disk

        var relative = LocalStoragePaths.Normalise(path);
        var fullPath = paths.ResolveFullPath(relative);
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

        logger.LogInformation("Stored file {Path}", relative);
        return relative;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = paths.ResolveFullPath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            logger.LogInformation("Deleted file {Path}", path);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public string GetPublicUrl(string path)
    {
        var relative = LocalStoragePaths.Normalise(path);
        return $"{application.Value.ApiBaseUrl}{paths.PublicRequestPath}/{LocalStoragePaths.ToUrlPath(relative)}";
    }

    private void TryDelete(string fullPath)
    {
        try { if (File.Exists(fullPath)) File.Delete(fullPath); }
        catch (Exception ex) { logger.LogWarning(ex, "Couldn't remove half-written file {FullPath}", fullPath); }
    }
}
