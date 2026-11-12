using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Quotes.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Quotes;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Quotes.Queries;

/// <summary>
/// The website's "Quote of the day" card: the quote for a UK date (see <see cref="QuoteRotation"/>).
/// </summary>
/// <param name="Date">The UK date (the endpoint passes today's, from <c>UkCalendar</c>).</param>
/// <remarks>The date is part of the cache key, so a new day gets a new cache entry and never yesterday's quote.</remarks>
[Cacheable(CacheTags.Quotes, Minutes = 60)]
public sealed record GetQuoteOfTheDayQuery(DateOnly Date) : IRequest<Result<QuoteOfTheDayDto>>;

/// <summary>Handles <see cref="GetQuoteOfTheDayQuery"/>.</summary>
public sealed class GetQuoteOfTheDayQueryHandler(IQuoteRepository quotes)
    : IRequestHandler<GetQuoteOfTheDayQuery, Result<QuoteOfTheDayDto>>
{
    /// <inheritdoc />
    public async Task<Result<QuoteOfTheDayDto>> Handle(GetQuoteOfTheDayQuery query, CancellationToken ct)
    {
        var published = await quotes.GetListAsync(publishedOnly: true, ct);
        var quote = QuoteRotation.Pick(published, query.Date);

        return quote is null
            ? Result.Failure<QuoteOfTheDayDto>(QuoteErrors.NoneToday)
            : Result.Success(QuoteOfTheDayDto.From(quote, query.Date));
    }
}
