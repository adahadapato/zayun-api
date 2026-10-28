using System.Security.Cryptography;
using ImageMagick;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.ValueObjects;

namespace Zayun.Infrastructure.Settings.Images;

/// <summary>
/// <see cref="IImageProcessor"/> on Magick.NET: reads JPEG, PNG, WebP or HEIC, fixes rotation, removes metadata,
/// shrinks to <see cref="ImageSettings.MaxSide"/>, makes a thumbnail, and writes both as WebP.
/// </summary>
/// <remarks>
/// <para>Transparency is kept (WebP supports it), so PNG screenshots with see-through corners stay that way.</para>
/// <para>Thread-safe: no state besides the settings.</para>
/// </remarks>
internal sealed class MagickImageProcessor(IOptions<ImageSettings> options, ILogger<MagickImageProcessor> logger)
    : IImageProcessor
{
    private const string OutputContentType = "image/webp";

    /// <summary>What we accept (decided from the file's contents, never its name).</summary>
    private static readonly HashSet<MagickFormat> AllowedFormats =
    [
        MagickFormat.Jpeg, MagickFormat.Jpg, MagickFormat.Pjpeg,
        MagickFormat.Png, MagickFormat.Png8, MagickFormat.Png24, MagickFormat.Png32,
        MagickFormat.WebP,
        MagickFormat.Heic, MagickFormat.Heif
    ];

    /// <inheritdoc />
    public async Task<Result<ProcessedImage>> ProcessAsync(Stream upload, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        // 1. Read, refusing anything over the limit without loading it all
        var bytes = await ReadLimitedAsync(upload, settings.MaxUploadBytes, cancellationToken);
        if (bytes is null) return ImageErrors.TooLarge(settings.MaxUploadBytes);
        if (bytes.Length == 0) return ImageErrors.Empty;

        // 2. Decode (upright, metadata stripped)
        var decoded = Decode(bytes);
        if (decoded.IsFailure) return decoded.Error;
        using var image = decoded.Value;

        // 3. Full size and thumbnail, both fitting inside their square (shape kept, never enlarged)
        using var thumbnail = image.Clone();
        FitInside(image, (uint)settings.MaxSide);
        FitInside(thumbnail, (uint)settings.ThumbnailMaxSide);

        // 4. WebP
        var full = Encode(image, settings.WebpQuality);
        var thumb = Encode(thumbnail, settings.WebpQuality);
        var dimensions = ImageDimensions.Create((int)image.Width, (int)image.Height);

        return new ProcessedImage(full, thumb, OutputContentType, dimensions, full.Length, Hash(full));
    }

    // =====================================================================
    // Steps
    // =====================================================================

    /// <summary>Reads the upload into memory; null when it's bigger than <paramref name="maxBytes"/>.</summary>
    private static async Task<byte[]?> ReadLimitedAsync(Stream upload, long maxBytes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(upload);
        if (upload.CanSeek && upload.Length - upload.Position > maxBytes) return null;

        using var buffer = new MemoryStream();
        var chunk = new byte[81_920];
        int read;
        while ((read = await upload.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > maxBytes) return null;
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    /// <summary>Opens the picture if it's an allowed format; applies rotation and removes metadata.</summary>
    private Result<MagickImage> Decode(byte[] bytes)
    {
        try
        {
            var info = new MagickImageInfo(bytes);
            if (!AllowedFormats.Contains(info.Format)) return ImageErrors.NotAnImage;

            var image = new MagickImage(bytes, new MagickReadSettings { Format = info.Format });
            image.AutoOrient();     // apply the camera's rotation flag to the pixels
            image.Strip();          // drop EXIF/GPS/ICC metadata
            return image;
        }
        catch (MagickException ex)
        {
            // Unreadable, corrupt, or over the resource limits: all mean "pick another file"
            logger.LogInformation(ex, "Upload rejected: not a readable image");
            return ImageErrors.NotAnImage;
        }
    }

    /// <summary>Shrinks to fit inside maxSide × maxSide, keeping the shape; leaves smaller pictures alone.</summary>
    private static void FitInside(IMagickImage<byte> image, uint maxSide)
    {
        if (image.Width > maxSide || image.Height > maxSide)
            image.Resize(new MagickGeometry(maxSide, maxSide));
    }

    /// <summary>Writes WebP into a stream positioned at the start.</summary>
    private static MemoryStream Encode(IMagickImage<byte> image, int quality)
    {
        image.Quality = (uint)quality;
        var output = new MemoryStream();
        image.Write(output, MagickFormat.WebP);
        output.Position = 0;
        return output;
    }

    /// <summary>SHA-256 of the stream's bytes as hex; leaves the stream at the start.</summary>
    private static string Hash(MemoryStream stream)
    {
        var hash = Convert.ToHexString(SHA256.HashData(stream.GetBuffer().AsSpan(0, (int)stream.Length)));
        stream.Position = 0;
        return hash;
    }
}
