using MediatR;
using Zayun.Application.Features.PersonalDetails.Commands;
using Zayun.Application.Features.PersonalDetails.Dtos;
using Zayun.Application.Features.PersonalDetails.Queries;
using Zayun.Domain.Constants;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.PersonalDetails;

/// <summary>
/// <c>/api/personal-details</c> (the website: only what's switched on) and <c>/api/admin/personal-details</c>
/// (everything, private parts included).
/// </summary>
public static class PersonalDetailsEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapPersonalDetailsEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/personal-details", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetPublicPersonalDetailsQuery(), ct)).ToHttp())
            .WithTags("Personal details")
            .WithName("GetPublicPersonalDetails")
            .WithSummary("Name, headline, public email, the phone numbers switched on, and town / country of the " +
                         "addresses switched on. Never street addresses or hidden numbers")
            .Produces<PublicPersonalDetailsDto>();

        var admin = api.MapGroup("/admin/personal-details")
                       .WithTags("Admin: Personal details")
                       .RequireAuthorization(Policies.RequireAdmin);

        admin.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetPersonalDetailsQuery(), ct)).ToHttp())
            .WithName("GetPersonalDetails")
            .WithSummary("Everything, private parts included")
            .Produces<PersonalDetailsDto>();

        admin.MapPut("/", async (SavePersonalDetailsCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .WithName("SavePersonalDetails")
            .WithSummary("Replace everything with what was typed in")
            .Produces<PersonalDetailsDto>()
            .ProducesValidationProblem();

        return api;
    }
}
