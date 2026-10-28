using Zayun.Domain.ValueObjects;

namespace Zayun.Domain.Interfaces;

/// <summary>
/// The one way processed pictures are stored, so uploads and the starter-data seeder always produce the same paths:
/// <c>{folder}/{imageId}.webp</c> and <c>{folder}/{imageId}_thumb.webp</c>.
/// </summary>
public static class FileStorageExtensions
{
    /// <summary>Saves a processed picture and its thumbnail.</summary>
    /// <param name="storage">The file storage.</param>
    /// <param name="image">The output of <see cref="IImageProcessor.ProcessAsync"/>.</param>
    /// <param name="folder">e.g. "projects/{projectId}" (forward slashes, no trailing slash).</param>
    /// <param name="imageId">The picture's Id, used as its file name.</param>
    /// <param name="cancellationToken">Cancels the writes.</param>
    /// <returns>What to record about the stored file.</returns>
    /// <remarks>If the thumbnail fails, the full-size file is deleted again, so nothing is left half-saved.</remarks>
    public static async Task<ImageFile> SaveImageAsync(this IFileStorage storage, ProcessedImage image, string folder,
                                                       Guid imageId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        var path = $"{folder.TrimEnd('/')}/{imageId:N}.webp";
        var thumbnailPath = $"{folder.TrimEnd('/')}/{imageId:N}_thumb.webp";

        await storage.SaveAsync(image.FullSize, path, image.ContentType, cancellationToken);
        try
        {
            await storage.SaveAsync(image.Thumbnail, thumbnailPath, image.ContentType, cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(path, CancellationToken.None);
            throw;
        }

        return ImageFile.Create(path, thumbnailPath, image.ContentType, image.FileSizeBytes, image.Dimensions,
                                image.Hash);
    }

    /// <summary>Deletes a stored picture and its thumbnail. Missing files are not an error.</summary>
    /// <param name="storage">The file storage.</param>
    /// <param name="file">The stored picture.</param>
    /// <param name="cancellationToken">Cancels the deletes.</param>
    public static async Task DeleteImageAsync(this IFileStorage storage, ImageFile file,
                                              CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(file);

        await storage.DeleteAsync(file.Path, cancellationToken);
        if (file.ThumbnailPath is not null) await storage.DeleteAsync(file.ThumbnailPath, cancellationToken);
    }
}
