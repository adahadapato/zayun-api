namespace Zayun.Domain.ValueObjects;

/// <summary>
/// A short animation stored as it was uploaded (an MP4 video, e.g. the "Nature first" bird → plane → drone loop).
/// Unlike pictures it isn't converted: the file is checked to really be an MP4 and kept within
/// <see cref="MaxFileSizeBytes"/>.
/// </summary>
/// <remarks>The website plays it silently on a loop, and shows the still picture instead to visitors who ask for
/// reduced motion, so an animation always comes with a picture.</remarks>
public sealed class AnimationFile : ValueObject
{
    /// <summary>The only kind accepted.</summary>
    public const string Mp4 = "video/mp4";

    /// <summary>Largest animation: 8 MB (a short loop is usually well under 1 MB).</summary>
    public const long MaxFileSizeBytes = 8 * 1024 * 1024;

    /// <summary>Longest stored path.</summary>
    public const int PathMaxLength = 500;

    /// <summary>Longest content type.</summary>
    public const int ContentTypeMaxLength = 100;

    /// <summary>Longest hash (SHA-256 as hex).</summary>
    public const int HashMaxLength = 64;

    /// <summary>Required by EF Core.</summary>
    private AnimationFile()
    {
        Path = string.Empty;
        ContentType = string.Empty;
        Hash = string.Empty;
    }

    private AnimationFile(string path, string contentType, long fileSizeBytes, string hash)
    {
        Path = path;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        Hash = hash;
    }

    /// <summary>Where it's stored (relative to the public uploads folder).</summary>
    public string Path { get; }

    /// <summary>Always <see cref="Mp4"/>.</summary>
    public string ContentType { get; }

    /// <summary>Its size.</summary>
    public long FileSizeBytes { get; }

    /// <summary>SHA-256 of the file, as hex.</summary>
    public string Hash { get; }

    /// <summary>Describes a stored animation.</summary>
    /// <param name="path">Where it's stored.</param>
    /// <param name="contentType">Must be <see cref="Mp4"/>.</param>
    /// <param name="fileSizeBytes">Its size (1 byte to <see cref="MaxFileSizeBytes"/>).</param>
    /// <param name="hash">SHA-256 as hex.</param>
    /// <returns>The value object.</returns>
    /// <exception cref="ArgumentException">Missing values or another content type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Too large, empty, or values too long.</exception>
    public static AnimationFile Create(string path, string contentType, long fileSizeBytes, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fileSizeBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fileSizeBytes, MaxFileSizeBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(path.Trim().Length, PathMaxLength, nameof(path));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hash.Trim().Length, HashMaxLength, nameof(hash));

        var type = contentType.Trim().ToLowerInvariant();
        if (type != Mp4) throw new ArgumentException("Only MP4 animations are stored.", nameof(contentType));

        return new AnimationFile(path.Trim(), type, fileSizeBytes, hash.Trim());
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Path;
        yield return ContentType;
        yield return FileSizeBytes;
        yield return Hash;
    }
}
