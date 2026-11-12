using MediatR;
using Microsoft.AspNetCore.Mvc;
using Zayun.Application.Common.Time;
using Zayun.Application.Features.Quotes.Commands;
using Zayun.Application.Features.Quotes.Dtos;
using Zayun.Application.Features.Quotes.Queries;
using Zayun.Domain.Constants;
using Zayun.Domain.Enums;
using Zayun.Presentation.Endpoints.Shared;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.Quotes;

/// <summary><c>/api/quotes</c> (the website) and <c>/api/admin/quotes</c> (editing and the "Coming up" preview).</summary>
public static class QuoteEndpoints
{
    /// <summary>How long a browser may keep today's quote: at most an hour (like the server's cache), and never past UK
    /// midnight, so an edit shows within the hour and tomorrow's quote shows straight after midnight.</summary>
    private static readonly TimeSpan BrowserCacheLimit = TimeSpan.FromHours(1);

    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapQuoteEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/quotes/today", async (ISender sender, TimeProvider clock, HttpContext http, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetQuoteOfTheDayQuery(UkCalendar.Today(clock)), ct);

                if (result.IsSuccess)
                {
                    var keepFor = UkCalendar.UntilMidnight(clock);
                    if (keepFor > BrowserCacheLimit) keepFor = BrowserCacheLimit;
                    http.Response.Headers.CacheControl = $"public, max-age={(int)keepFor.TotalSeconds}";
                }

                return result.ToHttp();
            })
            .WithTags("Quotes")
            .WithName("GetQuoteOfTheDay")
            .WithSummary("Today's quote (UK date): a quote pinned to today, otherwise the next one in the daily rotation")
            .Produces<QuoteOfTheDayDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        var admin = api.MapGroup("/admin/quotes")
                       .WithTags("Admin: Quotes")
                       .RequireAuthorization(Policies.RequireAdmin);

        admin.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAdminQuotesQuery(), ct)).ToHttp())
            .WithName("GetAdminQuotes")
            .WithSummary("Every quote, drafts included, in rotation order (canBeShown = published and source checked)")
            .Produces<IReadOnlyList<QuoteDto>>();

        admin.MapGet("/preview", async (DateOnly? from, int? days, ISender sender, TimeProvider clock,
                                        CancellationToken ct) =>
                (await sender.Send(new PreviewQuotesQuery(from ?? UkCalendar.Today(clock), days ?? 14), ct)).ToHttp())
            .WithName("PreviewQuotes")
            .WithSummary("Coming up: the quote shown on each day and why (from = UK date, default today; days 1–62, default 14)")
            .Produces<IReadOnlyList<QuotePreviewDayDto>>()
            .ProducesValidationProblem();

        admin.MapPost("/", async (CreateQuoteCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .WithName("CreateQuote")
            .WithSummary("Add a quote at the end of the rotation")
            .Produces<QuoteDto>()
            .ProducesValidationProblem();

        admin.MapPut("/{id:guid}", async (Guid id, QuoteRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateQuoteCommand(id, body.Kind, body.Text, body.Author, body.Source,
                                                          body.Language, body.Translation, body.Series, body.Story,
                                                          body.IsSourceChecked, body.PinnedMonth, body.PinnedDay),
                                   ct)).ToHttp())
            .WithName("UpdateQuote")
            .WithSummary("Change a quote")
            .Produces<QuoteDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/{id:guid}/picture", async (Guid id, IFormFile file, [FromForm] string altText,
                                                  ISender sender, CancellationToken ct) =>
            {
                await using var content = file.OpenReadStream();
                return (await sender.Send(new SetQuotePictureCommand(id, content, file.Length, altText), ct)).ToHttp();
            })
            .WithName("SetQuotePicture")
            .WithSummary("Upload or replace the still picture (multipart: file, altText); stored as WebP")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<QuoteDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .DisableAntiforgery();

        admin.MapDelete("/{id:guid}/picture", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RemoveQuotePictureCommand(id), ct)).ToHttp())
            .WithName("RemoveQuotePicture")
            .WithSummary("Remove the picture, and the animation with it (and their files)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/{id:guid}/animation", async (Guid id, IFormFile file, ISender sender, CancellationToken ct) =>
            {
                await using var content = file.OpenReadStream();
                return (await sender.Send(new SetQuoteAnimationCommand(id, content, file.Length), ct)).ToHttp();
            })
            .WithName("SetQuoteAnimation")
            .WithSummary("Upload or replace the MP4 animation (multipart: file; up to 8 MB; needs the picture first)")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<QuoteDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .DisableAntiforgery();

        admin.MapDelete("/{id:guid}/animation", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RemoveQuoteAnimationCommand(id), ct)).ToHttp())
            .WithName("RemoveQuoteAnimation")
            .WithSummary("Remove the animation (and its file); the picture stays")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/{id:guid}/published", async (Guid id, PublishedRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetQuotePublishedCommand(id, body.IsPublished), ct)).ToHttp())
            .WithName("SetQuotePublished")
            .WithSummary("Publish (true) or hide (false) a quote; it's only shown once its source is checked")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteQuoteCommand(id), ct)).ToHttp())
            .WithName("DeleteQuote")
            .WithSummary("Delete a quote")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPut("/order", async (OrderRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ReorderQuotesCommand(body.Ids), ct)).ToHttp())
            .WithName("ReorderQuotes")
            .WithSummary("Reorder the rotation: every quote's Id in the new order")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        return api;
    }

    /// <summary>A quote's editable fields (the Id is in the route).</summary>
    public sealed record QuoteRequest(QuoteKind Kind, string Text, string Author, string? Source, string? Language,
                                      string? Translation, string? Series, string? Story, bool IsSourceChecked,
                                      int? PinnedMonth, int? PinnedDay);
}
