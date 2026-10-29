using MediatR;
using Microsoft.AspNetCore.Mvc;
using Zayun.Application.Features.SiteImages.Commands;
using Zayun.Application.Features.SiteImages.Dtos;
using Zayun.Application.Features.SiteImages.Queries;
using Zayun.Domain.Constants;
using Zayun.Domain.Enums;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.SiteImages;

/// <summary>
/// <c>/api/site-images</c> (the website) and <c>/api/admin/site-images/{slot}</c> (upload, describe, remove), for
/// named pictures such as the owner's photo (slot <c>OwnerPhoto</c>).
/// </summary>
public static class SiteImageEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapSiteImageEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/site-images", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetSiteImagesQuery(), ct)).ToHttp())
            .WithTags("Site pictures")
            .WithName("GetSiteImages")
            .WithSummary("Named pictures on the website, e.g. the owner's photo (empty slots left out)")
            .Produces<IReadOnlyList<SiteImageDto>>();

        var admin = api.MapGroup("/admin/site-images")
                       .WithTags("Admin: Site pictures")
                       .RequireAuthorization(Policies.RequireAdmin);

        admin.MapPut("/{slot}", async (SiteImageSlot slot, IFormFile file, [FromForm] string altText,
                                       ISender sender, CancellationToken ct) =>
            {
                await using var content = file.OpenReadStream();
                return (await sender.Send(new SetSiteImageCommand(slot, content, file.Length, altText), ct)).ToHttp();
            })
            .WithName("SetSiteImage")
            .WithSummary("Upload or replace the picture in a slot (multipart: file, altText), e.g. PUT /OwnerPhoto")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<SiteImageDto>()
            .ProducesValidationProblem()
            .DisableAntiforgery();

        admin.MapPut("/{slot}/description", async (SiteImageSlot slot, DescriptionRequest body, ISender sender,
                                                   CancellationToken ct) =>
                (await sender.Send(new DescribeSiteImageCommand(slot, body.AltText), ct)).ToHttp())
            .WithName("DescribeSiteImage")
            .WithSummary("Change the picture's description (alt text)")
            .Produces<SiteImageDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/{slot}", async (SiteImageSlot slot, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RemoveSiteImageCommand(slot), ct)).ToHttp())
            .WithName("RemoveSiteImage")
            .WithSummary("Remove the picture (and its files)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    /// <summary>A new description.</summary>
    public sealed record DescriptionRequest(string AltText);
}
