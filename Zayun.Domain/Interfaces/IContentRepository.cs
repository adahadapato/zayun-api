using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>
/// What every website-content repository can do on top of <see cref="IRepository{TEntity}"/>: read the list in
/// order, load it all for reordering, and find the next free place.
/// </summary>
/// <typeparam name="TEntity">The content type.</typeparam>
public interface IContentRepository<TEntity> : IRepository<TEntity> where TEntity : ContentEntity
{
    /// <summary>The list in display order. Read-only (not tracked).</summary>
    /// <param name="publishedOnly">True for the website (drafts left out); false for the admin area.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<IReadOnlyList<TEntity>> GetListAsync(bool publishedOnly, CancellationToken cancellationToken = default);

    /// <summary>Every item, drafts included, tracked so their order can be changed.</summary>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<IReadOnlyList<TEntity>> GetAllForUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>The position for a new item: one after the last (0 for an empty list).</summary>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<int> GetNextDisplayOrderAsync(CancellationToken cancellationToken = default);
}
