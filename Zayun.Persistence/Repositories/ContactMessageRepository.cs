using Microsoft.EntityFrameworkCore;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IContactMessageRepository"/>.</summary>
internal sealed class ContactMessageRepository(ZayunDbContext context)
    : Repository<ContactMessage>(context), IContactMessageRepository
{
    /// <inheritdoc />
    public Task<PagedResult<ContactMessage>> GetPageAsync(ContactMessageStatus? status, PageRequest page,
                                                          CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking();
        if (status is not null) query = query.Where(m => m.Status == status);
        return ToPagedAsync(query.OrderByDescending(m => m.CreatedAt), page, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<ContactMessageStatus, int>> CountByStatusAsync(
        CancellationToken cancellationToken = default) =>
        await Set.GroupBy(m => m.Status)
                 .Select(g => new { g.Key, Count = g.Count() })
                 .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
}
