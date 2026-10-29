using MediatR;
using Zayun.Application.Features.Tags.Commands;
using Zayun.Application.Features.Tags.Dtos;
using Zayun.Application.Features.Tags.Queries;
using Zayun.Domain.Constants;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.Tags;

/// <summary>
/// <c>/api/admin/tags</c>: the shared tag list used by project tags, experience skills and skill-group skills, and
/// the tag check while typing.
/// </summary>
public static class TagEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapTagEndpoints(this IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin/tags")
                       .WithTags("Admin: Tags")
                       .RequireAuthorization(Policies.RequireAdmin);

        admin.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetTagsQuery(), ct)).ToHttp())
            .WithName("GetTags")
            .WithSummary("Every tag, by name, with how many projects, experience areas and skill groups use it")
            .Produces<IReadOnlyList<TagDto>>();

        admin.MapPost("/check", async (TagNameRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new CheckTagQuery(body.Name), ct)).ToHttp())
            .WithName("CheckTag")
            .WithSummary("Check a typed tag: Existing (maybe under another spelling), New (with its usual spelling), " +
                         "or Doubtful. Built-in rules first, then the AI for unknown names")
            .Produces<TagCheckDto>()
            .ProducesValidationProblem();

        admin.MapPost("/", async (TagNameRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new CreateTagCommand(body.Name), ct)).ToHttp())
            .WithName("CreateTag")
            .WithSummary("Add a tag to the list")
            .Produces<TagDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        admin.MapPut("/{id:guid}", async (Guid id, TagNameRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RenameTagCommand(id, body.Name), ct)).ToHttp())
            .WithName("RenameTag")
            .WithSummary("Rename a tag everywhere it's used (the old name is kept as an alias)")
            .Produces<TagDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admin.MapPost("/{id:guid}/merge", async (Guid id, MergeRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new MergeTagCommand(id, body.IntoTagId), ct)).ToHttp())
            .WithName("MergeTag")
            .WithSummary("Merge this tag into another: everything using it uses the other, and this one goes away")
            .Produces<TagDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteTagCommand(id), ct)).ToHttp())
            .WithName("DeleteTag")
            .WithSummary("Delete a tag nothing uses")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    /// <summary>A tag name.</summary>
    public sealed record TagNameRequest(string Name);

    /// <summary>The tag to merge into.</summary>
    public sealed record MergeRequest(Guid IntoTagId);
}
