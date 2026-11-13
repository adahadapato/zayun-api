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
public sealed class GetQuoteOfTheDayQueryHandler(IQuoteRepository quotes, IFileStorage storage)
    : IRequestHandler<GetQuoteOfTheDayQuery, Result<QuoteOfTheDayDto>>
{
    /// <inheritdoc />
    public async Task<Result<QuoteOfTheDayDto>> Handle(GetQuoteOfTheDayQuery query, CancellationToken ct)
    {
        var published = await quotes.GetListAsync(publishedOnly: true, ct);
        var quote = QuoteRotation.Pick(published, query.Date);

        return quote is null
            ? Result.Failure<QuoteOfTheDayDto>(QuoteErrors.NoneToday)
            : Result.Success(QuoteOfTheDayDto.From(quote, query.Date, storage));
    }
}

/// <summary>
/// The website's quote pages (<c>/quotes</c>): every quote the archive rule allows (<see cref="QuoteArchive"/>), the
/// ones shown most recently first. Also used for the quote sitemap.
/// </summary>
/// <param name="Today">Today's UK date (part of the cache key, so the list moves on each day).</param>
[Cacheable(CacheTags.Quotes, Minutes = 60)]
public sealed record GetQuoteArchiveQuery(DateOnly Today) : IRequest<Result<IReadOnlyList<ArchivedQuoteDto>>>;

/// <summary>Handles <see cref="GetQuoteArchiveQuery"/>.</summary>
public sealed class GetQuoteArchiveQueryHandler(IQuoteRepository quotes, IFileStorage storage)
    : IRequestHandler<GetQuoteArchiveQuery, Result<IReadOnlyList<ArchivedQuoteDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ArchivedQuoteDto>>> Handle(GetQuoteArchiveQuery query, CancellationToken ct)
    {
        var published = await quotes.GetListAsync(publishedOnly: true, ct);
        return Result.Success<IReadOnlyList<ArchivedQuoteDto>>(
            QuoteArchive.Build(published, query.Today).Select(e => ArchivedQuoteDto.From(e, storage)).ToList());
    }
}

/// <summary>
/// One quote's page (<c>/quotes/{slug}</c>). Not found when the archive rule hides it: a draft, an unchecked quote,
/// or a pinned / series quote whose day hasn't come yet.
/// </summary>
/// <param name="Slug">Its web address.</param>
/// <param name="Today">Today's UK date.</param>
[Cacheable(CacheTags.Quotes, Minutes = 60)]
public sealed record GetArchivedQuoteQuery(string Slug, DateOnly Today) : IRequest<Result<ArchivedQuoteDto>>;

/// <summary>Handles <see cref="GetArchivedQuoteQuery"/>.</summary>
public sealed class GetArchivedQuoteQueryHandler(IQuoteRepository quotes, IFileStorage storage)
    : IRequestHandler<GetArchivedQuoteQuery, Result<ArchivedQuoteDto>>
{
    /// <inheritdoc />
    public async Task<Result<ArchivedQuoteDto>> Handle(GetArchivedQuoteQuery query, CancellationToken ct)
    {
        var published = await quotes.GetListAsync(publishedOnly: true, ct);
        var entry = QuoteArchive.Build(published, query.Today)
                                .FirstOrDefault(e => string.Equals(e.Quote.Slug, query.Slug, StringComparison.Ordinal));

        return entry is null
            ? Result.Failure<ArchivedQuoteDto>(QuoteErrors.NotFound)
            : Result.Success(ArchivedQuoteDto.From(entry, storage));
    }
}

/// <summary>Admin → Quotes: every quote, drafts included, in rotation order. Never cached.</summary>
public sealed record GetAdminQuotesQuery : IRequest<Result<IReadOnlyList<QuoteDto>>>;

/// <summary>Handles <see cref="GetAdminQuotesQuery"/>.</summary>
public sealed class GetAdminQuotesQueryHandler(IQuoteRepository quotes, IFileStorage storage)
    : IRequestHandler<GetAdminQuotesQuery, Result<IReadOnlyList<QuoteDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<QuoteDto>>> Handle(GetAdminQuotesQuery query, CancellationToken ct) =>
        Result.Success<IReadOnlyList<QuoteDto>>(
            (await quotes.GetListAsync(publishedOnly: false, ct)).Select(q => QuoteDto.From(q, storage)).ToList());
}

/// <summary>
/// Admin → Quotes → "Coming up": which quote the website shows on each of the next days, and why. Uses exactly the
/// same rule as the website (<see cref="QuoteRotation"/>). Never cached.
/// </summary>
/// <param name="From">The first UK date.</param>
/// <param name="Days">How many days (1 to <see cref="MaxDays"/>).</param>
public sealed record PreviewQuotesQuery(DateOnly From, int Days) : IRequest<Result<IReadOnlyList<QuotePreviewDayDto>>>
{
    /// <summary>The longest preview.</summary>
    public const int MaxDays = 62;
}

/// <summary>Handles <see cref="PreviewQuotesQuery"/>.</summary>
public sealed class PreviewQuotesQueryHandler(IQuoteRepository quotes)
    : IRequestHandler<PreviewQuotesQuery, Result<IReadOnlyList<QuotePreviewDayDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<QuotePreviewDayDto>>> Handle(PreviewQuotesQuery query, CancellationToken ct)
    {
        var published = await quotes.GetListAsync(publishedOnly: true, ct);

        var days = Enumerable.Range(0, query.Days)
            .Select(offset =>
            {
                var date = query.From.AddDays(offset);
                var pick = QuoteRotation.PickWithReason(published, date);
                return pick is null
                    ? new QuotePreviewDayDto(date, null, null, null, null)
                    : new QuotePreviewDayDto(date, pick.Reason, pick.Quote.Id, pick.Quote.Text, pick.Quote.Author);
            })
            .ToList();

        return Result.Success<IReadOnlyList<QuotePreviewDayDto>>(days);
    }
}
