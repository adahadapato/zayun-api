using Zayun.Domain.Entities;
using Zayun.Domain.Enums;

namespace Zayun.Application.Features.Documents.Dtos;

/// <summary>A private document in the admin list (no link: ask for one with GetDocumentDownloadLink).</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Title">The name shown in the list.</param>
/// <param name="Category">CV, certificate…</param>
/// <param name="Notes">The private note.</param>
/// <param name="FileName">The name it downloads as.</param>
/// <param name="ContentType">PDF, Word, JPEG or PNG.</param>
/// <param name="FileSizeBytes">Its size.</param>
/// <param name="UploadedAt">When it was added (UTC).</param>
/// <param name="UpdatedAt">When it last changed (UTC), if ever.</param>
public sealed record DocumentDto(
    Guid Id,
    string Title,
    DocumentCategory Category,
    string? Notes,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTime UploadedAt,
    DateTime? UpdatedAt)
{
    /// <summary>Maps a document.</summary>
    /// <param name="d">The document.</param>
    /// <returns>The DTO.</returns>
    public static DocumentDto From(Document d) =>
        new(d.Id, d.Title, d.Category, d.Notes, d.File.OriginalFileName, d.File.ContentType, d.File.FileSizeBytes,
            d.CreatedAt, d.UpdatedAt);
}

/// <summary>A short-lived download link.</summary>
/// <param name="Url">Open this to download the file.</param>
/// <param name="ExpiresAt">When it stops working (UTC).</param>
public sealed record DocumentLinkDto(string Url, DateTime ExpiresAt);
