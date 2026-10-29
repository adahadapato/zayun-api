using MediatR;
using Zayun.Application.Features.Documents.Dtos;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Documents.Queries;

/// <summary>Admin → Documents: every document, newest first, optionally one category. Never cached.</summary>
/// <param name="Category">Only this category; null = all.</param>
public sealed record GetDocumentsQuery(DocumentCategory? Category = null) : IRequest<Result<IReadOnlyList<DocumentDto>>>;

/// <summary>Handles <see cref="GetDocumentsQuery"/>.</summary>
public sealed class GetDocumentsQueryHandler(IDocumentRepository documents)
    : IRequestHandler<GetDocumentsQuery, Result<IReadOnlyList<DocumentDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DocumentDto>>> Handle(GetDocumentsQuery query, CancellationToken ct) =>
        Result.Success<IReadOnlyList<DocumentDto>>(
            (await documents.GetListAsync(query.Category, ct)).Select(DocumentDto.From).ToList());
}

/// <summary>Admin → Documents → Open: a signed link that downloads the file for a few minutes.</summary>
/// <param name="Id">The document.</param>
public sealed record GetDocumentDownloadLinkQuery(Guid Id) : IRequest<Result<DocumentLinkDto>>;

/// <summary>Handles <see cref="GetDocumentDownloadLinkQuery"/>.</summary>
/// <remarks>Never cached: every request gets a fresh link.</remarks>
public sealed class GetDocumentDownloadLinkQueryHandler(IDocumentRepository documents, IPrivateFileStorage storage)
    : IRequestHandler<GetDocumentDownloadLinkQuery, Result<DocumentLinkDto>>
{
    /// <inheritdoc />
    public async Task<Result<DocumentLinkDto>> Handle(GetDocumentDownloadLinkQuery query, CancellationToken ct)
    {
        var document = await documents.GetByIdAsync(query.Id, ct);
        if (document is null) return DocumentErrors.NotFound;

        var link = storage.CreateDownloadLink(document.File.Path, document.File.OriginalFileName);
        return new DocumentLinkDto(link.Url.AbsoluteUri, link.ExpiresAt);
    }
}
