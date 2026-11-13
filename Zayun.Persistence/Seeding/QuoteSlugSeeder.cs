using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zayun.Domain.Shared.Quotes;

namespace Zayun.Persistence.Seeding;

/// <summary>
/// Gives a web address (<c>/quotes/{slug}</c>) to every quote that doesn't have one yet: the quotes stored before the
/// <c>Slug</c> column existed (migration <c>AddQuoteSlug</c> leaves them empty). New quotes get theirs when added.
/// </summary>
/// <remarks>Runs on every start but only changes anything the first time (afterwards no quote has an empty address).
/// Deleted quotes are included, so their old links stay theirs.</remarks>
internal static class QuoteSlugSeeder
{
    /// <summary>Fills in the missing addresses.</summary>
    /// <param name="context">The database.</param>
    /// <param name="logger">Start-up log.</param>
    /// <param name="cancellationToken">Stops seeding.</param>
    public static async Task SeedAsync(ZayunDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        var missing = await context.Quotes.IgnoreQueryFilters()
            .Where(q => q.Slug == "")
            .OrderBy(q => q.DisplayOrder).ThenBy(q => q.Id)
            .ToListAsync(cancellationToken);
        if (missing.Count == 0) return;

        var used = (await context.Quotes.IgnoreQueryFilters()
                .Where(q => q.Slug != "")
                .Select(q => q.Slug)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var quote in missing)
        {
            var slug = QuoteSlugs.Make(quote.Author, quote.Text, quote.Translation, used);
            quote.AssignSlug(slug);
            used.Add(slug);
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Gave web addresses to {Count} quotes", missing.Count);
    }
}
