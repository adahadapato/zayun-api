using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Zayun.Application.Features.Documents.Commands;
using Zayun.Application.Features.Documents.Dtos;
using Zayun.Application.Features.Documents.Queries;
using Zayun.Domain.Constants;
using Zayun.Domain.Enums;
using Zayun.Infrastructure.Settings.Storage;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.Documents;

/// <summary>
/// <c>/api/admin/documents</c> (the owner's private documents) and <c>/private-files/…</c> (downloads through signed,
/// expiring links).
/// </summary>
public static class DocumentEndpoints
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    /// <summary>Maps the admin routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder api)
    {
        var documents = api.MapGroup("/admin/documents")
                           .WithTags("Admin: Documents")
                           .RequireAuthorization(Policies.RequireAdmin);

        documents.MapGet("/", async (DocumentCategory? category, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetDocumentsQuery(category), ct)).ToHttp())
            .WithName("GetDocuments")
            .WithSummary("My private documents, newest first; optionally ?category=Cv|Certificate|Transcript|Paper|Identity|Contract|Other")
            .Produces<IReadOnlyList<DocumentDto>>();

        documents.MapPost("/", async (IFormFile file, [FromForm] string title, [FromForm] DocumentCategory category,
                                      [FromForm] string? notes, ISender sender, CancellationToken ct) =>
            {
                await using var content = file.OpenReadStream();
                return (await sender.Send(new UploadDocumentCommand(title, category, notes, content, file.FileName,
                                                                    file.Length), ct)).ToHttp();
            })
            .WithName("UploadDocument")
            .WithSummary("Upload a private document (multipart: file, title, category, notes)")
            .WithDescription("PDF, Word (.docx), JPEG or PNG, up to 20 MB; the type is checked from the file's contents. " +
                             "Stored as is, outside the website's folders.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<DocumentDto>()
            .ProducesValidationProblem()
            .DisableAntiforgery();

        documents.MapPut("/{id:guid}", async (Guid id, DocumentRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateDocumentCommand(id, body.Title, body.Category, body.Notes), ct)).ToHttp())
            .WithName("UpdateDocument")
            .WithSummary("Change a document's title, category and note")
            .Produces<DocumentDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        documents.MapPut("/{id:guid}/file", async (Guid id, IFormFile file, ISender sender, CancellationToken ct) =>
            {
                await using var content = file.OpenReadStream();
                return (await sender.Send(new ReplaceDocumentFileCommand(id, content, file.FileName, file.Length), ct))
                    .ToHttp();
            })
            .WithName("ReplaceDocumentFile")
            .WithSummary("Upload a new version of the file (multipart: file); the old file is deleted")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<DocumentDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .DisableAntiforgery();

        documents.MapGet("/{id:guid}/link", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetDocumentDownloadLinkQuery(id), ct)).ToHttp())
            .WithName("GetDocumentDownloadLink")
            .WithSummary("A signed link that downloads the file for a few minutes, then stops working")
            .Produces<DocumentLinkDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        documents.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteDocumentCommand(id), ct)).ToHttp())
            .WithName("DeleteDocument")
            .WithSummary("Delete a document for good (the row and its file)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    /// <summary>
    /// Maps <c>/private-files/{**path}?name=&amp;expires=&amp;sig=</c>: streams a private document when the link is
    /// valid. The signature IS the permission, so no sign-in is needed (a browser can't send the token on a plain
    /// link); a wrong, expired or tampered link looks exactly like a missing file.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication MapPrivateFiles(this WebApplication app)
    {
        var paths = app.Services.GetRequiredService<LocalStoragePaths>();

        app.MapGet($"{paths.PrivateRequestPath}/{{**path}}",
                (string path, string? name, long expires, string? sig, PrivateFileLinks links, HttpContext context) =>
                {
                    if (!links.TryResolve(path, name, expires, sig, out var fullPath))
                        return Results.NotFound();

                    if (!ContentTypes.TryGetContentType(fullPath, out var contentType))
                        contentType = "application/octet-stream";

                    context.Response.Headers.CacheControl = "private, no-store";
                    context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
                    return Results.File(fullPath, contentType, fileDownloadName: name, enableRangeProcessing: true);
                })
            .AllowAnonymous()
            .ExcludeFromDescription();

        return app;
    }

    /// <summary>A document's editable fields (the Id is in the route).</summary>
    public sealed record DocumentRequest(string Title, DocumentCategory Category, string? Notes);
}
