using Microsoft.EntityFrameworkCore;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Persistence.Repositories;

/// <summary>
/// Base for every repository: loading one aggregate by Id, adding and removing, plus the paging helper.
/// Repositories never save; <see cref="IUnitOfWork.SaveChangesAsync"/> does, once, at the end of a use case.
/// </summary>
/// <typeparam name="TEntity">The aggregate root.</typeparam>
/// <remarks>
/// <para><b>Whole aggregates:</b> override <see cref="WithChildren"/> to <c>Include</c> the children the Domain needs
/// (e.g. a project's pictures), so every method that returns the aggregate returns ALL of it.</para>
/// <para><b>Tracking:</b> entities returned for changing are tracked (the default). Pure read-only lists use
/// <c>AsNoTracking()</c> for speed.</para>
/// <para><b>Soft delete:</b> the global filter already hides deleted rows; use <c>IgnoreQueryFilters()</c> only where a
/// method's contract says deleted rows count (e.g. "is this name taken?").</para>
/// </remarks>
internal abstract class Repository<TEntity>(ZayunDbContext context) : IRepository<TEntity>
    where TEntity : Entity<Guid>
{
    /// <summary>The shared, request-scoped database context.</summary>
    protected ZayunDbContext Context { get; } = context;

    /// <summary>This aggregate's table.</summary>
    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    /// <summary>Adds the aggregate's children to a query. The default adds nothing.</summary>
    /// <param name="query">The query to extend.</param>
    /// <returns>The query with the needed <c>Include</c>s.</returns>
    protected virtual IQueryable<TEntity> WithChildren(IQueryable<TEntity> query) => query;

    /// <inheritdoc />
    public virtual Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithChildren(Set).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public void Add(TEntity entity) => Set.Add(entity);

    /// <inheritdoc />
    public void Remove(TEntity entity) => Set.Remove(entity);

    /// <summary>Runs a query as one page: a COUNT plus the requested slice.</summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="query">An ORDERED query (paging without an order gives random pages).</param>
    /// <param name="page">Which page and how many per page.</param>
    /// <param name="cancellationToken">Cancels the database calls.</param>
    /// <returns>The page.</returns>
    protected static async Task<PagedResult<T>> ToPagedAsync<T>(IQueryable<T> query, PageRequest page,
                                                                CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        if (total == 0) return PagedResult<T>.Empty(page.Page, page.PageSize);

        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, total, page.Page, page.PageSize);
    }
}