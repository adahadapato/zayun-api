using Microsoft.EntityFrameworkCore;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IQuoteRepository"/>.</summary>
internal sealed class QuoteRepository(ZayunDbContext context)
    : ContentRepository<Quote>(context), IQuoteRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> GetUsedSlugsAsync(CancellationToken cancellationToken = default) =>
        (await Set.IgnoreQueryFilters()
                  .Where(q => q.Slug != "")
                  .Select(q => q.Slug)
                  .ToListAsync(cancellationToken))
        .ToHashSet(StringComparer.Ordinal);
}
