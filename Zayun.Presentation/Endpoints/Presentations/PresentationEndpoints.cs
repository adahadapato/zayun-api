using MediatR;
using Microsoft.AspNetCore.Mvc;
using Zayun.Application.Features.Presentations.Commands;
using Zayun.Application.Features.Presentations.Dtos;
using Zayun.Application.Features.Presentations.Queries;
using Zayun.Domain.Constants;
using Zayun.Domain.Enums;
using Zayun.Presentation.Endpoints.Shared;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.Presentations;

/// <summary><c>/api/presentations</c> (the website) and <c>/api/admin/presentations</c> (editing, with a picture).</summary>
public static class PresentationEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapPresentationEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/presentations", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetPresentationsQuery(), ct)).ToHttp())
            .WithTags("Research")
            .WithName("GetPresentations")
            .WithSummary("Published presentations in order, with their pictures")
            .Produces<IReadOnlyList<PresentationDto>>();

        var admin = api.MapGroup("/admin/presentations")
                       .WithTags("Admin: Presentations")
                       .RequireAuthorization(Policies.RequireAdmin);

        admin.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAdminPresentationsQuery(), ct)).ToHttp())
            .WithName("GetAdminPresentations")
            .WithSummary("Every presentation, drafts included, in order")
            .Produces<IReadOnlyList<PresentationDto>>();

        admin.MapPost("/", async (CreatePresentationCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .WithName("CreatePresentation")
            .WithSummary("Add a presentation at the end of the list; add its picture next")
            .Produces<PresentationDto>()
            .ProducesValidationProblem();

        admin.MapPut("/{id:guid}", async (Guid id, PresentationRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdatePresentationCommand(id, body.Title, body.EventName, body.Year, body.Type,
                                                                 body.ImageAlt), ct)).ToHttp())
            .WithName("UpdatePresentation")
            .WithSummary("Change a presentation (and its picture's alt text, if given)")
            .Produces<PresentationDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/{id:guid}/image", async (Guid id, IFormFile file, [FromForm] string altText,
                                                ISender sender, CancellationToken ct) =>
            {
                await using var content = file.OpenReadStream();
                return (await sender.Send(new SetPresentationImageCommand(id, content, file.Length, altText), ct)).ToHttp();
            })
            .WithName("SetPresentationImage")
            .WithSummary("Upload or replace the poster, slide or certificate (multipart: file, altText)")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<PresentationDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .DisableAntiforgery();

        admin.MapDelete("/{id:guid}/image", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RemovePresentationImageCommand(id), ct)).ToHttp())
            .WithName("RemovePresentationImage")
            .WithSummary("Remove the picture (and its files)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/{id:guid}/published", async (Guid id, PublishedRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetPresentationPublishedCommand(id, body.IsPublished), ct)).ToHttp())
            .WithName("SetPresentationPublished")
            .WithSummary("Show (true) or hide (false) a presentation")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeletePresentationCommand(id), ct)).ToHttp())
            .WithName("DeletePresentation")
            .WithSummary("Delete a presentation")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/order", async (OrderRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ReorderPresentationsCommand(body.Ids), ct)).ToHttp())
            .WithName("ReorderPresentations")
            .WithSummary("Drag-to-reorder: every presentation's Id in the new order")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        return api;
    }

    /// <summary>A presentation's editable fields (the Id is in the route).</summary>
    public sealed record PresentationRequest(string Title, string EventName, int Year, PresentationType Type,
                                             string? ImageAlt);
}
