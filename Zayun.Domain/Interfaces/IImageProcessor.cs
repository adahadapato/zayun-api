using Zayun.Domain.Shared.Results;
using Zayun.Domain.ValueObjects;

namespace Zayun.Domain.Interfaces;

/// <summary>
/// The image pipeline (Infrastructure, Magick.NET). Does NOT store anything: it returns processed bytes, and the
/// use case saves them with <see cref="IFileStorage"/>. Accepts JPEG, PNG, WebP and HEIC; always outputs WebP.
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Fixes rotation, shrinks to the configured maximum (never enlarges), makes a thumbnail, converts both to WebP
    /// and strips metadata (camera details, GPS).
    /// </summary>
    /// <param name="upload">The uploaded file.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    /// <returns>The processed image; or ImageErrors.Empty, NotAnImage or TooLarge.</returns>
    Task<Result<ProcessedImage>> ProcessAsync(Stream upload, CancellationToken cancellationToken = default);
}

/// <summary>What the pipeline produces. Streams are owned by the caller: dispose after saving.</summary>
/// <param name="FullSize">The resized picture.</param>
/// <param name="Thumbnail">The small version for cards.</param>
/// <param name="ContentType">Always "image/webp".</param>
/// <param name="Dimensions">Size of the full picture.</param>
/// <param name="FileSizeBytes">Size of the full picture's file.</param>
/// <param name="Hash">SHA-256 of the full picture, as hex.</param>
public sealed record ProcessedImage(
    Stream FullSize,
    Stream Thumbnail,
    string ContentType,
    ImageDimensions Dimensions,
    long FileSizeBytes,
    string Hash) : IAsyncDisposable
{
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await FullSize.DisposeAsync();
        await Thumbnail.DisposeAsync();
    }
}
