namespace Zayun.Domain.ValueObjects;

/// <summary>
/// A private document stored exactly as uploaded (never resized or converted). Only a signed-in admin can open it,
/// through a short-lived signed link.
/// </summary>
/// <remarks>EF Core builds it through the private parameterless constructor and then fills the properties, the same
/// way as <see cref="ImageFile"/>. Code always goes through <see cref="Create"/>.</remarks>
public sealed class DocumentFile : ValueObject
{
    /// <summary>Largest upload: 20 MB.</summary>
    public const long MaxFileSizeBytes = 20 * 1024 * 1024;

    /// <summary>A PDF.</summary>
    public const string Pdf = "application/pdf";

    /// <summary>A Word document (.docx).</summary>
    public const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    /// <summary>A JPEG picture.</summary>
    public const string Jpeg = "image/jpeg";

    /// <summary>A PNG picture.</summary>
    public const string Png = "image/png";

    /// <summary>What can be uploaded, with the file extension each is stored under.</summary>
    public static readonly IReadOnlyDictionary<string, string> AllowedTypes = new Dictionary<string, string>
    {
        [Pdf] = "pdf",
        [Docx] = "docx",
        [Jpeg] = "jpg",
        [Png] = "png"
    };

    /// <summary>Longest storage path (column size).</summary>
    public const int PathMaxLength = 500;

    /// <summary>Longest original file name (column size).</summary>
    public const int OriginalFileNameMaxLength = 255;

    /// <summary>Longest content type (column size).</summary>
    public const int ContentTypeMaxLength = 100;

    /// <summary>Longest hash: SHA-256 written as hex is 64 characters.</summary>
    public const int HashMaxLength = 64;

    /// <summary>Required by EF Core; never call this from code.</summary>
    private DocumentFile()
    {
        Path = string.Empty;
        OriginalFileName = string.Empty;
        ContentType = string.Empty;
        Hash = string.Empty;
    }

    private DocumentFile(string path, string originalFileName, string contentType, long fileSizeBytes, string hash)
    {
        Path = path;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        Hash = hash;
    }

    /// <summary>Where it lives in private storage, e.g. "documents/{documentId}/{fileId}.pdf".</summary>
    public string Path { get; }

    /// <summary>The uploaded file's name, used again when it's downloaded ("CV 2026.pdf").</summary>
    public string OriginalFileName { get; }

    /// <summary>One of <see cref="AllowedTypes"/>.</summary>
    public string ContentType { get; }

    /// <summary>File size in bytes.</summary>
    public long FileSizeBytes { get; }

    /// <summary>SHA-256 of the file, as hex.</summary>
    public string Hash { get; }

    /// <summary>Records a stored document.</summary>
    /// <param name="path">Storage path.</param>
    /// <param name="originalFileName">The uploaded file's name (with its extension).</param>
    /// <param name="contentType">One of <see cref="AllowedTypes"/>.</param>
    /// <param name="fileSizeBytes">1 byte to <see cref="MaxFileSizeBytes"/>.</param>
    /// <param name="hash">SHA-256 as hex.</param>
    /// <returns>The value object.</returns>
    /// <exception cref="ArgumentException">Blank text or a type that isn't allowed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A bad size, or text longer than its column.</exception>
    public static DocumentFile Create(string path, string originalFileName, string contentType,
                                      long fileSizeBytes, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fileSizeBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fileSizeBytes, MaxFileSizeBytes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(path.Trim().Length, PathMaxLength, nameof(path));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(originalFileName.Trim().Length, OriginalFileNameMaxLength,
                                                       nameof(originalFileName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hash.Trim().Length, HashMaxLength, nameof(hash));

        var type = contentType.Trim().ToLowerInvariant();
        if (!AllowedTypes.ContainsKey(type))
            throw new ArgumentException("Not an allowed document type.", nameof(contentType));

        return new DocumentFile(path.Trim(), originalFileName.Trim(), type, fileSizeBytes,
                                hash.Trim().ToUpperInvariant());
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Path;
        yield return OriginalFileName;
        yield return ContentType;
        yield return FileSizeBytes;
        yield return Hash;
    }
}
