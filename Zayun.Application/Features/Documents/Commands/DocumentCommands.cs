using MediatR;
using Microsoft.Extensions.Logging;
using Zayun.Application.Common.Documents;
using Zayun.Application.Features.Documents.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Domain.ValueObjects;

namespace Zayun.Application.Features.Documents.Commands;

/// <summary>Admin → Documents → Upload. Stored as is (never converted), in private storage.</summary>
/// <param name="Title">The name shown in the list.</param>
/// <param name="Category">CV, certificate…</param>
/// <param name="Notes">Optional private note.</param>
/// <param name="Content">The uploaded file.</param>
/// <param name="FileName">Its name, kept for downloading.</param>
/// <param name="SizeBytes">Its size, as sent.</param>
public sealed record UploadDocumentCommand(string Title, DocumentCategory Category, string? Notes,
                                           Stream Content, string? FileName, long SizeBytes)
    : IRequest<Result<DocumentDto>>;

/// <summary>Handles <see cref="UploadDocumentCommand"/>.</summary>
/// <remarks>The file is saved first; if the row can't be saved, the file is deleted again.</remarks>
public sealed class UploadDocumentCommandHandler(
    IDocumentRepository documents,
    IUnitOfWork unitOfWork,
    IPrivateFileStorage storage,
    ILogger<UploadDocumentCommandHandler> logger)
    : IRequestHandler<UploadDocumentCommand, Result<DocumentDto>>
{
    /// <inheritdoc />
    public async Task<Result<DocumentDto>> Handle(UploadDocumentCommand c, CancellationToken ct)
    {
        var id = Guid.CreateVersion7();
        var stored = await DocumentUploads.StoreAsync(storage, c.Content, c.FileName, c.Title, id, ct);
        if (stored.IsFailure) return stored.Error;

        try
        {
            var document = Document.Create(id, c.Title, c.Category, c.Notes, stored.Value);
            documents.Add(document);
            await unitOfWork.SaveChangesAsync(ct);
            logger.LogInformation("Stored private document {DocumentId} ({Category})", id, c.Category);
            return DocumentDto.From(document);
        }
        catch
        {
            await DocumentFiles.DeleteQuietlyAsync(storage, stored.Value, logger);
            throw;
        }
    }
}

/// <summary>Admin → Documents: change the title, category and note.</summary>
/// <param name="Id">The document.</param>
/// <param name="Title">The name shown in the list.</param>
/// <param name="Category">CV, certificate…</param>
/// <param name="Notes">Optional private note.</param>
public sealed record UpdateDocumentCommand(Guid Id, string Title, DocumentCategory Category, string? Notes)
    : IRequest<Result<DocumentDto>>;

/// <summary>Handles <see cref="UpdateDocumentCommand"/>.</summary>
public sealed class UpdateDocumentCommandHandler(IDocumentRepository documents, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateDocumentCommand, Result<DocumentDto>>
{
    /// <inheritdoc />
    public async Task<Result<DocumentDto>> Handle(UpdateDocumentCommand c, CancellationToken ct)
    {
        var document = await documents.GetByIdAsync(c.Id, ct);
        if (document is null) return DocumentErrors.NotFound;

        document.Update(c.Title, c.Category, c.Notes);
        await unitOfWork.SaveChangesAsync(ct);
        return DocumentDto.From(document);
    }
}

/// <summary>Admin → Documents: upload a new version of the file (e.g. an updated CV). The old file is deleted.</summary>
/// <param name="Id">The document.</param>
/// <param name="Content">The new file.</param>
/// <param name="FileName">Its name, kept for downloading.</param>
/// <param name="SizeBytes">Its size, as sent.</param>
public sealed record ReplaceDocumentFileCommand(Guid Id, Stream Content, string? FileName, long SizeBytes)
    : IRequest<Result<DocumentDto>>;

/// <summary>Handles <see cref="ReplaceDocumentFileCommand"/>.</summary>
/// <remarks>The new file is saved first; the old one is deleted only after the change is saved.</remarks>
public sealed class ReplaceDocumentFileCommandHandler(
    IDocumentRepository documents,
    IUnitOfWork unitOfWork,
    IPrivateFileStorage storage,
    ILogger<ReplaceDocumentFileCommandHandler> logger)
    : IRequestHandler<ReplaceDocumentFileCommand, Result<DocumentDto>>
{
    /// <inheritdoc />
    public async Task<Result<DocumentDto>> Handle(ReplaceDocumentFileCommand c, CancellationToken ct)
    {
        var document = await documents.GetByIdAsync(c.Id, ct);
        if (document is null) return DocumentErrors.NotFound;

        var stored = await DocumentUploads.StoreAsync(storage, c.Content, c.FileName, document.Title, document.Id, ct);
        if (stored.IsFailure) return stored.Error;

        DocumentFile previous;
        try
        {
            previous = document.ReplaceFile(stored.Value);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            await DocumentFiles.DeleteQuietlyAsync(storage, stored.Value, logger);
            throw;
        }

        await DocumentFiles.DeleteQuietlyAsync(storage, previous, logger);
        return DocumentDto.From(document);
    }
}

/// <summary>Admin → Documents: delete a document for good (the row and its file).</summary>
/// <param name="Id">The document.</param>
public sealed record DeleteDocumentCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteDocumentCommand"/>.</summary>
public sealed class DeleteDocumentCommandHandler(
    IDocumentRepository documents,
    IUnitOfWork unitOfWork,
    IPrivateFileStorage storage,
    ILogger<DeleteDocumentCommandHandler> logger)
    : IRequestHandler<DeleteDocumentCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeleteDocumentCommand c, CancellationToken ct)
    {
        var document = await documents.GetByIdAsync(c.Id, ct);
        if (document is null) return DocumentErrors.NotFound;

        documents.Remove(document);
        await unitOfWork.SaveChangesAsync(ct);
        await DocumentFiles.DeleteQuietlyAsync(storage, document.File, logger);
        return Result.Success();
    }
}

/// <summary>Deleting files that are no longer used; a failure is only logged.</summary>
internal static class DocumentFiles
{
    public static async Task DeleteQuietlyAsync(IPrivateFileStorage storage, DocumentFile file, ILogger logger)
    {
        try
        {
            await storage.DeleteAsync(file.Path, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Couldn't delete private file {Path}.", file.Path);
        }
    }
}
