using MediatR;
using Zayun.Application.Features.Experience.Commands;
using Zayun.Application.Features.Experience.Dtos;
using Zayun.Application.Features.Experience.Queries;
using Zayun.Domain.Constants;
using Zayun.Presentation.Endpoints.Shared;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.Experience;

/// <summary><c>/api/experience</c> (the website) and <c>/api/admin/experience</c> (editing).</summary>
public static class ExperienceEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapExperienceEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/experience", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetExperienceQuery(), ct)).ToHttp())
            .WithTags("Experience")
            .WithName("GetExperience")
            .WithSummary("Published experience areas in order")
            .Produces<IReadOnlyList<ExperienceAreaDto>>();

        var admin = api.MapGroup("/admin/experience")
                       .WithTags("Admin: Experience")
                       .RequireAuthorization(Policies.RequireAdmin);

        admin.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAdminExperienceQuery(), ct)).ToHttp())
            .WithName("GetAdminExperience")
            .WithSummary("Every experience area, drafts included, in order")
            .Produces<IReadOnlyList<ExperienceAreaDto>>();

        admin.MapPost("/", async (CreateExperienceAreaCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .WithName("CreateExperienceArea")
            .WithSummary("Add an experience area at the end of the list")
            .Produces<ExperienceAreaDto>()
            .ProducesValidationProblem();

        admin.MapPut("/{id:guid}", async (Guid id, TextGroupRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateExperienceAreaCommand(id, body.Title, body.Description, body.Skills), ct))
                    .ToHttp())
            .WithName("UpdateExperienceArea")
            .WithSummary("Change an experience area's text and skills")
            .Produces<ExperienceAreaDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/{id:guid}/published", async (Guid id, PublishedRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetExperienceAreaPublishedCommand(id, body.IsPublished), ct)).ToHttp())
            .WithName("SetExperienceAreaPublished")
            .WithSummary("Show (true) or hide (false) an experience area")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteExperienceAreaCommand(id), ct)).ToHttp())
            .WithName("DeleteExperienceArea")
            .WithSummary("Delete an experience area")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/order", async (OrderRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ReorderExperienceAreasCommand(body.Ids), ct)).ToHttp())
            .WithName("ReorderExperienceAreas")
            .WithSummary("Drag-to-reorder: every experience area's Id in the new order")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        return api;
    }
}
