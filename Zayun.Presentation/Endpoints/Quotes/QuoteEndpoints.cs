using MediatR;
using Zayun.Application.Common.Time;
using Zayun.Application.Features.Quotes.Dtos;
using Zayun.Application.Features.Quotes.Queries;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.Quotes;

/// <summary><c>/api/quotes</c> (the website). The admin routes (<c>/api/admin/quotes</c>) come in the next step.</summary>
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

        return api;
    }
}
