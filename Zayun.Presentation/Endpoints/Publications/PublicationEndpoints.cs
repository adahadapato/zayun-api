using MediatR;
using Zayun.Application.Features.Publications.Commands;
using Zayun.Application.Features.Publications.Dtos;
using Zayun.Application.Features.Publications.Queries;
using Zayun.Domain.Constants;
using Zayun.Domain.Enums;
using Zayun.Presentation.Endpoints.Shared;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.Publications;

/// <summary><c>/api/publications</c> (the website) and <c>/api/admin/publications</c> (editing).</summary>
public static class PublicationEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapPublicationEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/publications", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetPublicationsQuery(), ct)).ToHttp())
            .WithTags("Research")
            .WithName("GetPublications")
            .WithSummary("Published publications in order, with their links")
            .Produces<IReadOnlyList<PublicationDto>>();

        var admin = api.MapGroup("/admin/publications")
                       .WithTags("Admin: Publications")
                       .RequireAuthorization(Policies.RequireAdmin);

        admin.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAdminPublicationsQuery(), ct)).ToHttp())
            .WithName("GetAdminPublications")
            .WithSummary("Every publication, drafts included, in order")
            .Produces<IReadOnlyList<PublicationDto>>();

        admin.MapPost("/", async (CreatePublicationCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .WithName("CreatePublication")
            .WithSummary("Add a publication at the end of the list")
            .Produces<PublicationDto>()
            .ProducesValidationProblem();

        admin.MapPut("/{id:guid}", async (Guid id, PublicationRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdatePublicationCommand(id, body.Title, body.Authors, body.Venue, body.Year,
                                                                body.Type, body.Award, body.Links), ct)).ToHttp())
            .WithName("UpdatePublication")
            .WithSummary("Change a publication (its links are replaced with the ones sent)")
            .Produces<PublicationDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/{id:guid}/published", async (Guid id, PublishedRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetPublicationPublishedCommand(id, body.IsPublished), ct)).ToHttp())
            .WithName("SetPublicationPublished")
            .WithSummary("Show (true) or hide (false) a publication")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeletePublicationCommand(id), ct)).ToHttp())
            .WithName("DeletePublication")
            .WithSummary("Delete a publication")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/order", async (OrderRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ReorderPublicationsCommand(body.Ids), ct)).ToHttp())
            .WithName("ReorderPublications")
            .WithSummary("Drag-to-reorder: every publication's Id in the new order")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        return api;
    }

    /// <summary>A publication's editable fields (the Id is in the route).</summary>
    public sealed record PublicationRequest(
        string Title,
        string Authors,
        string Venue,
        int Year,
        PublicationType Type,
        string? Award,
        IReadOnlyList<PublicationLinkDto>? Links);
}
