using MediatR;
using Zayun.Application.Features.Skills.Commands;
using Zayun.Application.Features.Skills.Dtos;
using Zayun.Application.Features.Skills.Queries;
using Zayun.Domain.Constants;
using Zayun.Presentation.Endpoints.Shared;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.Skills;

/// <summary><c>/api/skills</c> (the website) and <c>/api/admin/skills</c> (skill groups and core expertise).</summary>
public static class SkillEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapSkillEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/skills", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetSkillsQuery(), ct)).ToHttp())
            .WithTags("Skills")
            .WithName("GetSkills")
            .WithSummary("The Skills page: published skill groups and core expertise, in order")
            .Produces<SkillsPageDto>();

        var admin = api.MapGroup("/admin/skills")
                       .WithTags("Admin: Skills")
                       .RequireAuthorization(Policies.RequireAdmin);

        admin.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAdminSkillsQuery(), ct)).ToHttp())
            .WithName("GetAdminSkills")
            .WithSummary("Every skill group and core expertise item, drafts included")
            .Produces<SkillsPageDto>();

        MapGroups(admin.MapGroup("/groups"));
        MapExpertise(admin.MapGroup("/expertise"));
        return api;
    }

    // ───────────────────────────── Skill groups ─────────────────────────────

    private static void MapGroups(RouteGroupBuilder groups)
    {
        groups.MapPost("/", async (CreateSkillGroupCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .WithName("CreateSkillGroup")
            .WithSummary("Add a skill group at the end of the list")
            .Produces<SkillGroupDto>()
            .ProducesValidationProblem();

        groups.MapPut("/{id:guid}", async (Guid id, TextGroupRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateSkillGroupCommand(id, body.Title, body.Description, body.Skills), ct))
                    .ToHttp())
            .WithName("UpdateSkillGroup")
            .WithSummary("Change a skill group's text and skills")
            .Produces<SkillGroupDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        groups.MapPut("/{id:guid}/published", async (Guid id, PublishedRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetSkillGroupPublishedCommand(id, body.IsPublished), ct)).ToHttp())
            .WithName("SetSkillGroupPublished")
            .WithSummary("Show (true) or hide (false) a skill group")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        groups.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteSkillGroupCommand(id), ct)).ToHttp())
            .WithName("DeleteSkillGroup")
            .WithSummary("Delete a skill group")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        groups.MapPut("/order", async (OrderRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ReorderSkillGroupsCommand(body.Ids), ct)).ToHttp())
            .WithName("ReorderSkillGroups")
            .WithSummary("Drag-to-reorder: every skill group's Id in the new order")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();
    }

    // ───────────────────────────── Core expertise ─────────────────────────────

    private static void MapExpertise(RouteGroupBuilder expertise)
    {
        expertise.MapPost("/", async (CreateCoreExpertiseCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .WithName("CreateCoreExpertise")
            .WithSummary("Add a core expertise item at the end of the list")
            .Produces<CoreExpertiseDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        expertise.MapPut("/{id:guid}", async (Guid id, NameRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RenameCoreExpertiseCommand(id, body.Name), ct)).ToHttp())
            .WithName("RenameCoreExpertise")
            .WithSummary("Rename a core expertise item")
            .Produces<CoreExpertiseDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        expertise.MapPut("/{id:guid}/published", async (Guid id, PublishedRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetCoreExpertisePublishedCommand(id, body.IsPublished), ct)).ToHttp())
            .WithName("SetCoreExpertisePublished")
            .WithSummary("Show (true) or hide (false) a core expertise item")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        expertise.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteCoreExpertiseCommand(id), ct)).ToHttp())
            .WithName("DeleteCoreExpertise")
            .WithSummary("Delete a core expertise item")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        expertise.MapPut("/order", async (OrderRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ReorderCoreExpertiseCommand(body.Ids), ct)).ToHttp())
            .WithName("ReorderCoreExpertise")
            .WithSummary("Drag-to-reorder: every core expertise Id in the new order")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();
    }
}
