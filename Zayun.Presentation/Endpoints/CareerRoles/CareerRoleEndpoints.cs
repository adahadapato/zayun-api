using MediatR;
using Zayun.Application.Features.CareerRoles.Commands;
using Zayun.Application.Features.CareerRoles.Dtos;
using Zayun.Application.Features.CareerRoles.Queries;
using Zayun.Domain.Constants;
using Zayun.Domain.Enums;
using Zayun.Presentation.Endpoints.Shared;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.CareerRoles;

/// <summary><c>/api/career-roles</c> (the website's "Roles" and the CVs) and <c>/api/admin/career-roles</c> (editing).</summary>
/// <remarks>Roles are ordered by their dates (current first, then newest), so there's no reorder route.</remarks>
public static class CareerRoleEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapCareerRoleEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/career-roles", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetCareerRolesQuery(), ct)).ToHttp())
            .WithTags("Roles")
            .WithName("GetCareerRoles")
            .WithSummary("Published roles: current first, then newest")
            .Produces<IReadOnlyList<CareerRoleDto>>();

        var admin = api.MapGroup("/admin/career-roles")
                       .WithTags("Admin: Roles")
                       .RequireAuthorization(Policies.RequireAdmin);

        admin.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAdminCareerRolesQuery(), ct)).ToHttp())
            .WithName("GetAdminCareerRoles")
            .WithSummary("Every role, drafts included, current first, then newest")
            .Produces<IReadOnlyList<CareerRoleDto>>();

        admin.MapPost("/", async (CreateCareerRoleCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .WithName("CreateCareerRole")
            .WithSummary("Add a role")
            .Produces<CareerRoleDto>()
            .ProducesValidationProblem();

        admin.MapPut("/{id:guid}", async (Guid id, CareerRoleRequest b, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateCareerRoleCommand(id, b.Kind, b.Title, b.Organisation, b.Location,
                                                               b.StartYear, b.StartMonth, b.EndYear, b.EndMonth,
                                                               b.Summary, b.Highlights), ct)).ToHttp())
            .WithName("UpdateCareerRole")
            .WithSummary("Change a role's details")
            .Produces<CareerRoleDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/{id:guid}/published", async (Guid id, PublishedRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetCareerRolePublishedCommand(id, body.IsPublished), ct)).ToHttp())
            .WithName("SetCareerRolePublished")
            .WithSummary("Show (true) or hide (false) a role")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteCareerRoleCommand(id), ct)).ToHttp())
            .WithName("DeleteCareerRole")
            .WithSummary("Delete a role")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    /// <summary>The body of PUT <c>/api/admin/career-roles/{id}</c>: every detail (the Id comes from the address).</summary>
    public sealed record CareerRoleRequest(CareerRoleKind Kind, string Title, string Organisation, string? Location,
                                           int StartYear, int? StartMonth, int? EndYear, int? EndMonth,
                                           string? Summary, IReadOnlyList<string>? Highlights);
}
