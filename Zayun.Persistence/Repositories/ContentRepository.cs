using Microsoft.EntityFrameworkCore;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>
/// Base for every website-content repository: the ordered list (website or admin), loading everything for a reorder,
/// and the next free position.
/// </summary>
/// <typeparam name="TEntity">The content type.</typeparam>
/// <remarks>Order: <see cref="ContentEntity.DisplayOrder"/>, then oldest first, so equal positions stay stable.</remarks>
internal abstract class ContentRepository<TEntity>(ZayunDbContext context)
    : Repository<TEntity>(context), IContentRepository<TEntity>
    where TEntity : ContentEntity
{
    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> GetListAsync(bool publishedOnly,
                                                                   CancellationToken cancellationToken = default) =>
        await InOrder(Published(WithChildren(Set.AsNoTracking()), publishedOnly)).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> GetAllForUpdateAsync(CancellationToken cancellationToken = default) =>
        await InOrder(Set).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<int> GetNextDisplayOrderAsync(CancellationToken cancellationToken = default) =>
        (await Set.MaxAsync(e => (int?)e.DisplayOrder, cancellationToken) ?? -1) + 1;

    /// <summary>Leaves drafts out when only published items are wanted.</summary>
    /// <param name="query">The query.</param>
    /// <param name="publishedOnly">True for the website.</param>
    /// <returns>The filtered query.</returns>
    protected static IQueryable<TEntity> Published(IQueryable<TEntity> query, bool publishedOnly) =>
        publishedOnly ? query.Where(e => e.IsPublished) : query;

    /// <summary>The list's order.</summary>
    /// <param name="query">The query.</param>
    /// <returns>The ordered query.</returns>
    protected static IOrderedQueryable<TEntity> InOrder(IQueryable<TEntity> query) =>
        query.OrderBy(e => e.DisplayOrder).ThenBy(e => e.CreatedAt);
}
