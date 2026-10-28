using Microsoft.EntityFrameworkCore;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IProjectRepository"/>. Every project comes with its pictures.</summary>
internal sealed class ProjectRepository(ZayunDbContext context)
    : ContentRepository<Project>(context), IProjectRepository
{
    /// <inheritdoc />
    protected override IQueryable<Project> WithChildren(IQueryable<Project> query) => query.Include(p => p.Images);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Project>> GetListAsync(ProjectCategory? category, bool publishedOnly,
                                                           CancellationToken cancellationToken = default)
    {
        var query = Published(WithChildren(Set.AsNoTracking()), publishedOnly);
        if (category is not null) query = query.Where(p => p.Category == category);
        return await InOrder(query).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Project?> GetBySlugAsync(string slug, bool publishedOnly, CancellationToken cancellationToken = default) =>
        Published(WithChildren(Set.AsNoTracking()), publishedOnly)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

    /// <inheritdoc />
    /// <remarks>Deleted projects don't count, matching the filtered unique index.</remarks>
    public Task<bool> SlugExistsAsync(string slug, Guid? exceptProjectId, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(p => p.Slug == slug && (exceptProjectId == null || p.Id != exceptProjectId), cancellationToken);
}
