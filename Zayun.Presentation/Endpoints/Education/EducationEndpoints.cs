using MediatR;
using Zayun.Application.Features.Education.Commands;
using Zayun.Application.Features.Education.Dtos;
using Zayun.Application.Features.Education.Queries;
using Zayun.Domain.Constants;
using Zayun.Presentation.Endpoints.Shared;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.Education;

/// <summary><c>/api/education</c> (the website's "Education" and the CVs) and <c>/api/admin/education</c> (editing).</summary>
/// <remarks>Qualifications are ordered by year (in progress first, then newest), so there's no reorder route.</remarks>
public static class EducationEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapEducationEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/education", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetEducationQuery(), ct)).ToHttp())
            .WithTags("Education")
            .WithName("GetEducation")
            .WithSummary("Published qualifications: in progress first, then newest")
            .Produces<IReadOnlyList<QualificationDto>>();

        var admin = api.MapGroup("/admin/education")
                       .WithTags("Admin: Education")
                       .RequireAuthorization(Policies.RequireAdmin);

        admin.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAdminEducationQuery(), ct)).ToHttp())
            .WithName("GetAdminEducation")
            .WithSummary("Every qualification, drafts included, in progress first, then newest")
            .Produces<IReadOnlyList<QualificationDto>>();

        admin.MapPost("/", async (CreateQualificationCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .WithName("CreateQualification")
            .WithSummary("Add a qualification")
            .Produces<QualificationDto>()
            .ProducesValidationProblem();

        admin.MapPut("/{id:guid}", async (Guid id, QualificationRequest b, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateQualificationCommand(id, b.Degree, b.Subject, b.Institution, b.Location,
                                                                  b.Year, b.Note), ct)).ToHttp())
            .WithName("UpdateQualification")
            .WithSummary("Change a qualification's details")
            .Produces<QualificationDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/{id:guid}/published", async (Guid id, PublishedRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetQualificationPublishedCommand(id, body.IsPublished), ct)).ToHttp())
            .WithName("SetQualificationPublished")
            .WithSummary("Show (true) or hide (false) a qualification")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteQualificationCommand(id), ct)).ToHttp())
            .WithName("DeleteQualification")
            .WithSummary("Delete a qualification")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    /// <summary>The body of PUT <c>/api/admin/education/{id}</c>: every detail (the Id comes from the address).</summary>
    public sealed record QualificationRequest(string Degree, string Subject, string Institution, string? Location,
                                              int? Year, string? Note);
}
