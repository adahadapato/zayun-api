using Microsoft.EntityFrameworkCore;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ITagRepository"/>.</summary>
/// <remarks>Tags are loaded TRACKED: renames and merges change them in place.</remarks>
internal sealed class TagRepository(ZayunDbContext context) : Repository<Tag>(context), ITagRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Set.OrderBy(t => t.Name).ToListAsync(cancellationToken);
}
