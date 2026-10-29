using MediatR;
using Microsoft.AspNetCore.Mvc;
using Zayun.Application.Features.Awards.Commands;
using Zayun.Application.Features.Awards.Dtos;
using Zayun.Application.Features.Awards.Queries;
using Zayun.Domain.Constants;
using Zayun.Presentation.Endpoints.Shared;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.Awards;

/// <summary><c>/api/awards</c> (the website) and <c>/api/admin/awards</c> (editing, with a picture).</summary>
public static class AwardEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapAwardEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/awards", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAwardsQuery(), ct)).ToHttp())
            .WithTags("Awards")
            .WithName("GetAwards")
            .WithSummary("Published awards in order, with their pictures (isFeatured = also on About and Experience)")
            .Produces<IReadOnlyList<AwardDto>>();

        var admin = api.MapGroup("/admin/awards")
                       .WithTags("Admin: Awards")
                       .RequireAuthorization(Policies.RequireAdmin);

        admin.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAdminAwardsQuery(), ct)).ToHttp())
            .WithName("GetAdminAwards")
            .WithSummary("Every award, drafts included, in order")
            .Produces<IReadOnlyList<AwardDto>>();

        admin.MapPost("/", async (CreateAwardCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .WithName("CreateAward")
            .WithSummary("Add an award at the end of the list; add its picture next")
            .Produces<AwardDto>()
            .ProducesValidationProblem();

        admin.MapPut("/{id:guid}", async (Guid id, AwardRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateAwardCommand(id, body.Title, body.AwardedBy, body.Year, body.Description,
                                                          body.Url, body.IsFeatured, body.ImageAlt), ct)).ToHttp())
            .WithName("UpdateAward")
            .WithSummary("Change an award (and its picture's alt text, if given)")
            .Produces<AwardDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/{id:guid}/image", async (Guid id, IFormFile file, [FromForm] string altText,
                                                ISender sender, CancellationToken ct) =>
            {
                await using var content = file.OpenReadStream();
                return (await sender.Send(new SetAwardImageCommand(id, content, file.Length, altText), ct)).ToHttp();
            })
            .WithName("SetAwardImage")
            .WithSummary("Upload or replace the certificate or photo (multipart: file, altText)")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<AwardDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .DisableAntiforgery();

        admin.MapDelete("/{id:guid}/image", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RemoveAwardImageCommand(id), ct)).ToHttp())
            .WithName("RemoveAwardImage")
            .WithSummary("Remove the picture (and its files)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/{id:guid}/published", async (Guid id, PublishedRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetAwardPublishedCommand(id, body.IsPublished), ct)).ToHttp())
            .WithName("SetAwardPublished")
            .WithSummary("Show (true) or hide (false) an award")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteAwardCommand(id), ct)).ToHttp())
            .WithName("DeleteAward")
            .WithSummary("Delete an award")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/order", async (OrderRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ReorderAwardsCommand(body.Ids), ct)).ToHttp())
            .WithName("ReorderAwards")
            .WithSummary("Drag-to-reorder: every award's Id in the new order")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        return api;
    }

    /// <summary>An award's editable fields (the Id is in the route).</summary>
    public sealed record AwardRequest(string Title, string AwardedBy, int Year, string Description, string? Url,
                                      bool IsFeatured, string? ImageAlt);
}
