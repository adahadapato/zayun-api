using System.IO.Compression;
using System.Security.Cryptography;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.ValueObjects;

namespace Zayun.Application.Common.Documents;

/// <summary>
/// Checks and stores an uploaded private document: its REAL type from its contents (never its name), its size, its
/// hash, and a clean file name for downloading.
/// </summary>
internal static class DocumentUploads
{
    /// <summary>Reads, checks and stores an upload.</summary>
    /// <param name="storage">Private storage.</param>
    /// <param name="content">The uploaded file.</param>
    /// <param name="fileName">The name it had on the owner's computer (may be empty).</param>
    /// <param name="fallbackName">Used when the name is empty, e.g. the document's title.</param>
    /// <param name="documentId">The document (its folder).</param>
    /// <param name="ct">Cancels the work.</param>
    /// <returns>What to record about the stored file; or DocumentErrors.Empty, TooLarge or InvalidFormat.</returns>
    public static async Task<Result<DocumentFile>> StoreAsync(IPrivateFileStorage storage, Stream content,
                                                              string? fileName, string fallbackName, Guid documentId,
                                                              CancellationToken ct)
    {
        // 1. Read it, refusing anything over the limit without loading it all
        var bytes = await ReadLimitedAsync(content, DocumentFile.MaxFileSizeBytes, ct);
        if (bytes is null) return DocumentErrors.TooLarge;
        if (bytes.Length == 0) return DocumentErrors.Empty;

        // 2. What it really is
        var contentType = DetectContentType(bytes);
        if (contentType is null) return DocumentErrors.InvalidFormat;
        var extension = DocumentFile.AllowedTypes[contentType];

        // 3. Store it under a new name (the original name is kept only for downloading)
        var path = $"documents/{documentId:N}/{Guid.CreateVersion7():N}.{extension}";
        using (var stream = new MemoryStream(bytes, writable: false))
            await storage.SaveAsync(stream, path, ct);

        return DocumentFile.Create(path, CleanFileName(fileName, fallbackName, extension), contentType, bytes.Length,
                                   Convert.ToHexString(SHA256.HashData(bytes)));
    }

    /// <summary>Reads the upload into memory; null when it's bigger than <paramref name="maxBytes"/>.</summary>
    private static async Task<byte[]?> ReadLimitedAsync(Stream upload, long maxBytes, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(upload);
        if (upload.CanSeek && upload.Length - upload.Position > maxBytes) return null;

        using var buffer = new MemoryStream();
        var chunk = new byte[81_920];
        int read;
        while ((read = await upload.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > maxBytes) return null;
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    /// <summary>The file's type from its first bytes; null when it isn't one we accept.</summary>
    /// <remarks>A .docx is a zip, so a zip is only accepted when it really holds a Word document.</remarks>
    internal static string? DetectContentType(byte[] bytes)
    {
        ReadOnlySpan<byte> head = bytes.AsSpan(0, Math.Min(bytes.Length, 8));

        if (head.StartsWith("%PDF"u8)) return DocumentFile.Pdf;
        if (head.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF])) return DocumentFile.Jpeg;
        if (head.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) return DocumentFile.Png;
        if (head.StartsWith((ReadOnlySpan<byte>)[0x50, 0x4B, 0x03, 0x04]) && IsWordDocument(bytes)) return DocumentFile.Docx;
        return null;
    }

    private static bool IsWordDocument(byte[] bytes)
    {
        try
        {
            using var zip = new ZipArchive(new MemoryStream(bytes, writable: false), ZipArchiveMode.Read);
            return zip.GetEntry("[Content_Types].xml") is not null && zip.GetEntry("word/document.xml") is not null;
        }
        catch (InvalidDataException)
        {
            return false;   // a damaged zip
        }
    }

    /// <summary>A safe download name: no folders or odd characters, the right extension, not too long.</summary>
    internal static string CleanFileName(string? fileName, string fallback, string extension)
    {
        // Browsers send just the name, but some send a full path ("C:\Users\…\CV.pdf"): keep the last part either way
        var lastPart = (fileName ?? string.Empty).Replace('\\', '/').Split('/').Last();
        var name = Path.GetFileNameWithoutExtension(lastPart).Trim();
        if (string.IsNullOrWhiteSpace(name)) name = fallback.Trim();

        var invalid = Path.GetInvalidFileNameChars().Concat(['"', '<', '>', '|', ':', '*', '?', '\\', '/']).ToHashSet();
        name = new string(name.Select(c => invalid.Contains(c) || char.IsControl(c) ? '_' : c).ToArray()).Trim();
        if (name.Length == 0) name = "document";

        var maxNameLength = DocumentFile.OriginalFileNameMaxLength - extension.Length - 1;
        if (name.Length > maxNameLength) name = name[..maxNameLength];
        return $"{name}.{extension}";
    }
}
