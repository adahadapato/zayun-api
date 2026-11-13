using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves quotes for the "Quote of the day" card and the quote pages. Implemented in Persistence.</summary>
public interface IQuoteRepository : IContentRepository<Quote>
{
    /// <summary>Every web address already used, deleted quotes included (so an old link never leads to a different
    /// quote). Used when a new quote gets its address.</summary>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<IReadOnlySet<string>> GetUsedSlugsAsync(CancellationToken cancellationToken = default);
}
