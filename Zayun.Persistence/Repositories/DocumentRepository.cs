using Microsoft.EntityFrameworkCore;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IDocumentRepository"/>.</summary>
internal sealed class DocumentRepository(ZayunDbContext context)
    : Repository<Document>(context), IDocumentRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Document>> GetListAsync(DocumentCategory? category,
                                                            CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking();
        if (category is not null) query = query.Where(d => d.Category == category);
        return await query.OrderByDescending(d => d.CreatedAt).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Set.CountAsync(cancellationToken);
}
