using MediatR;
using Microsoft.AspNetCore.Mvc;
using Zayun.Application.Features.Projects.Commands;
using Zayun.Application.Features.Projects.Dtos;
using Zayun.Application.Features.Projects.Queries;
using Zayun.Domain.Constants;
using Zayun.Domain.Enums;
using Zayun.Presentation.Endpoints.Shared;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.Projects;

/// <summary><c>/api/projects</c> (the website) and <c>/api/admin/projects</c> (editing, with pictures).</summary>
public static class ProjectEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder api)
    {
        MapPublic(api);
        MapAdmin(api);
        return api;
    }

    // ═════════════════════════════ A. The website ═════════════════════════════

    private static void MapPublic(IEndpointRouteBuilder api)
    {
        var projects = api.MapGroup("/projects").WithTags("Projects");

        projects.MapGet("/", async (ProjectCategory? category, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetProjectsQuery(category), ct)).ToHttp())
            .WithName("GetProjects")
            .WithSummary("Published projects in order, with their pictures; optionally ?category=Featured or Engineering")
            .Produces<IReadOnlyList<ProjectDto>>();

        projects.MapGet("/{slug}", async (string slug, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetProjectBySlugQuery(slug), ct)).ToHttp())
            .WithName("GetProjectBySlug")
            .WithSummary("One published project by its web address, e.g. /api/projects/rccg-hope-house")
            .Produces<ProjectDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    // ═════════════════════════════ B. Admin ═════════════════════════════

    private static void MapAdmin(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin/projects")
                       .WithTags("Admin: Projects")
                       .RequireAuthorization(Policies.RequireAdmin);

        // ───────────────────────────── 1. Projects ─────────────────────────────

        admin.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAdminProjectsQuery(), ct)).ToHttp())
            .WithName("GetAdminProjects")
            .WithSummary("Every project, drafts included, in order")
            .Produces<IReadOnlyList<ProjectDto>>();

        admin.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAdminProjectQuery(id), ct)).ToHttp())
            .WithName("GetAdminProject")
            .WithSummary("One project, draft or not")
            .Produces<ProjectDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPost("/", async (CreateProjectCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .WithName("CreateProject")
            .WithSummary("Add a project at the end of the list (slug empty = made from the title); add pictures next")
            .Produces<ProjectDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        admin.MapPut("/{id:guid}", async (Guid id, ProjectRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(body.ToCommand(id), ct)).ToHttp())
            .WithName("UpdateProject")
            .WithSummary("Change a project's text, links, tags and web address")
            .Produces<ProjectDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admin.MapPut("/{id:guid}/published", async (Guid id, PublishedRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetProjectPublishedCommand(id, body.IsPublished), ct)).ToHttp())
            .WithName("SetProjectPublished")
            .WithSummary("Show (true) or hide (false) a project on the website")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteProjectCommand(id), ct)).ToHttp())
            .WithName("DeleteProject")
            .WithSummary("Delete a project")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/order", async (OrderRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ReorderProjectsCommand(body.Ids), ct)).ToHttp())
            .WithName("ReorderProjects")
            .WithSummary("Drag-to-reorder: every project's Id (drafts included) in the new order")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        // ───────────────────────────── 2. Pictures ─────────────────────────────

        admin.MapPost("/{id:guid}/images", async (Guid id, IFormFile file, [FromForm] string altText,
                                                  [FromForm] string? label, [FromForm] ProjectImageKind kind,
                                                  ISender sender, CancellationToken ct) =>
            {
                await using var content = file.OpenReadStream();
                return (await sender.Send(new AddProjectImageCommand(id, content, file.Length, altText, label, kind), ct))
                    .ToHttp();
            })
            .WithName("AddProjectImage")
            .WithSummary("Upload a picture (multipart: file, altText, label, kind = Primary | Secondary | Gallery)")
            .WithDescription("JPEG, PNG, WebP or HEIC. Resized and stored as WebP with a thumbnail.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ProjectImageDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .DisableAntiforgery();

        admin.MapPut("/{id:guid}/images/{imageId:guid}",
                async (Guid id, Guid imageId, ProjectImageRequest body, ISender sender, CancellationToken ct) =>
                    (await sender.Send(new UpdateProjectImageCommand(id, imageId, body.AltText, body.Label, body.Kind), ct))
                        .ToHttp())
            .WithName("UpdateProjectImage")
            .WithSummary("Change a picture's alt text, caption and kind")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admin.MapPut("/{id:guid}/images/{imageId:guid}/file",
                async (Guid id, Guid imageId, IFormFile file, ISender sender, CancellationToken ct) =>
                {
                    await using var content = file.OpenReadStream();
                    return (await sender.Send(new ReplaceProjectImageFileCommand(id, imageId, content, file.Length), ct))
                        .ToHttp();
                })
            .WithName("ReplaceProjectImageFile")
            .WithSummary("Change a picture's file, keeping its place, alt text, caption and kind (multipart: file)")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ProjectImageDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .DisableAntiforgery();

        admin.MapDelete("/{id:guid}/images/{imageId:guid}",
                async (Guid id, Guid imageId, ISender sender, CancellationToken ct) =>
                    (await sender.Send(new DeleteProjectImageCommand(id, imageId), ct)).ToHttp())
            .WithName("DeleteProjectImage")
            .WithSummary("Remove a picture (and its files)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/{id:guid}/images/order", async (Guid id, OrderRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ReorderProjectImagesCommand(id, body.Ids), ct)).ToHttp())
            .WithName("ReorderProjectImages")
            .WithSummary("Drag-to-reorder the pictures: every picture Id in the new order")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    // ═════════════════════════════ Request bodies ═════════════════════════════

    /// <summary>A project's editable fields (the Id is in the route).</summary>
    public sealed record ProjectRequest(
        string Title,
        string? Slug,
        string Subtitle,
        string Description,
        ProjectCategory Category,
        string Label,
        string? VisualLabel,
        string? GithubUrl,
        string? LiveUrl,
        bool IsPrivateRepo,
        IReadOnlyList<string>? Tags)
    {
        /// <summary>The update command for this project.</summary>
        public UpdateProjectCommand ToCommand(Guid id) =>
            new(id, Title, Slug, Subtitle, Description, Category, Label, VisualLabel, GithubUrl, LiveUrl,
                IsPrivateRepo, Tags);
    }

    /// <summary>A picture's editable fields.</summary>
    public sealed record ProjectImageRequest(string AltText, string? Label, ProjectImageKind Kind);
}
