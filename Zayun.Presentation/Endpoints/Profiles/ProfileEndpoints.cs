using MediatR;
using Zayun.Application.Features.Profiles.Commands;
using Zayun.Application.Features.Profiles.Dtos;
using Zayun.Application.Features.Profiles.Queries;
using Zayun.Domain.Constants;
using Zayun.Domain.Enums;
using Zayun.Presentation.Endpoints.Shared;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.Profiles;

/// <summary><c>/api/profiles</c> (the website) and <c>/api/admin/profiles</c> (editing).</summary>
public static class ProfileEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/profiles", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetProfilesQuery(), ct)).ToHttp())
            .WithTags("Profiles")
            .WithName("GetProfiles")
            .WithSummary("Published online profiles in order, with where each is shown (footer, contact, research)")
            .Produces<IReadOnlyList<ProfileDto>>();

        var admin = api.MapGroup("/admin/profiles")
                       .WithTags("Admin: Profiles")
                       .RequireAuthorization(Policies.RequireAdmin);

        admin.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAdminProfilesQuery(), ct)).ToHttp())
            .WithName("GetAdminProfiles")
            .WithSummary("Every profile, drafts included, in order")
            .Produces<IReadOnlyList<ProfileDto>>();

        admin.MapPost("/", async (CreateProfileCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .WithName("CreateProfile")
            .WithSummary("Add a profile at the end of the list")
            .Produces<ProfileDto>()
            .ProducesValidationProblem();

        admin.MapPut("/{id:guid}", async (Guid id, ProfileRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateProfileCommand(id, body.Platform, body.Name, body.Url, body.Label,
                                                            body.ShowInFooter, body.ShowOnContact, body.ShowOnResearch),
                                   ct)).ToHttp())
            .WithName("UpdateProfile")
            .WithSummary("Change a profile")
            .Produces<ProfileDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/{id:guid}/published", async (Guid id, PublishedRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetProfilePublishedCommand(id, body.IsPublished), ct)).ToHttp())
            .WithName("SetProfilePublished")
            .WithSummary("Show (true) or hide (false) a profile")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteProfileCommand(id), ct)).ToHttp())
            .WithName("DeleteProfile")
            .WithSummary("Delete a profile")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/order", async (OrderRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ReorderProfilesCommand(body.Ids), ct)).ToHttp())
            .WithName("ReorderProfiles")
            .WithSummary("Reorder: every profile's Id in the new order")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        return api;
    }

    /// <summary>A profile's editable fields (the Id is in the route).</summary>
    public sealed record ProfileRequest(ProfilePlatform Platform, string Name, string Url, string? Label,
                                        bool ShowInFooter, bool ShowOnContact, bool ShowOnResearch);
}
