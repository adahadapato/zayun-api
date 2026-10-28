namespace Zayun.Domain.ValueObjects;

/// <summary>
/// Facts about an image file that has already been saved to storage
/// (the file itself is NOT in the database, only where it is and what it is).
/// Validated once here, so every image owner (project screenshots, presentation posters) shares the checks.
/// </summary>
public sealed class ImageFile : ValueObject
{
    /// <summary>Formats the app accepts (uploads are converted to WebP).</summary>
    public static readonly IReadOnlyList<string> AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    /// <summary>Shown when the format isn't allowed.</summary>
    public const string InvalidFormatMessage = "Please upload a JPEG, PNG or WebP photo.";

    /// <summary>Longest storage path for the file or its thumbnail (column size).</summary>
    public const int PathMaxLength = 500;

    /// <summary>Longest content type (column size); the allowed ones are far shorter.</summary>
    public const int ContentTypeMaxLength = 100;

    /// <summary>Longest hash: SHA-256 written as hex is 64 characters.</summary>
    public const int HashMaxLength = 64;

    /// <summary>
    /// Required by EF Core, which can't pass <see cref="Dimensions"/> into a constructor.
    /// EF fills the properties straight after; never call this from code.
    /// </summary>
    private ImageFile()
    {
        Path = string.Empty;
        ContentType = string.Empty;
        Hash = string.Empty;
        Dimensions = null!;
    }

    private ImageFile(string path, string? thumbnailPath, string contentType,
                      long fileSizeBytes, ImageDimensions dimensions, string hash)
    {
        Path = path;
        ThumbnailPath = thumbnailPath;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        Dimensions = dimensions;
        Hash = hash;
    }

    /// <summary>Where the full-size file lives, e.g. "projects/{projectId}/{imageId}.webp".</summary>
    public string Path { get; }

    /// <summary>Small version for cards and lists.</summary>
    public string? ThumbnailPath { get; }

    /// <summary>"image/jpeg", "image/png" or "image/webp".</summary>
    public string ContentType { get; }

    /// <summary>File size in bytes.</summary>
    public long FileSizeBytes { get; }

    /// <summary>Width × height in pixels.</summary>
    public ImageDimensions Dimensions { get; }

    /// <summary>SHA-256 of the file (worked out by Infrastructure). Used to spot the same photo uploaded twice.</summary>
    public string Hash { get; }

    /// <summary>Records a stored image.</summary>
    /// <param name="path">Storage path of the full-size file.</param>
    /// <param name="thumbnailPath">Storage path of the thumbnail, if any.</param>
    /// <param name="contentType">One of <see cref="AllowedContentTypes"/>.</param>
    /// <param name="fileSizeBytes">More than 0.</param>
    /// <param name="dimensions">Width × height.</param>
    /// <param name="hash">SHA-256 as hex.</param>
    /// <returns>The value object.</returns>
    /// <exception cref="ArgumentException">Blank text or a format that isn't allowed.</exception>
    /// <exception cref="ArgumentNullException">No dimensions.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Size 0 or less, or text longer than its column.</exception>
    public static ImageFile Create(string path, string? thumbnailPath, string contentType,
                                   long fileSizeBytes, ImageDimensions dimensions, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        ArgumentNullException.ThrowIfNull(dimensions);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fileSizeBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(path.Trim().Length, PathMaxLength, nameof(path));
        if (!string.IsNullOrWhiteSpace(thumbnailPath))
            ArgumentOutOfRangeException.ThrowIfGreaterThan(thumbnailPath.Trim().Length, PathMaxLength, nameof(thumbnailPath));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hash.Trim().Length, HashMaxLength, nameof(hash));

        var type = contentType.Trim().ToLowerInvariant();
        if (!AllowedContentTypes.Contains(type))
            throw new ArgumentException(InvalidFormatMessage, nameof(contentType));

        return new ImageFile(path.Trim(),
            string.IsNullOrWhiteSpace(thumbnailPath) ? null : thumbnailPath.Trim(),
            type, fileSizeBytes, dimensions, hash.Trim().ToUpperInvariant());
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Path;
        yield return ThumbnailPath;
        yield return ContentType;
        yield return FileSizeBytes;
        yield return Dimensions;
        yield return Hash;
    }
}