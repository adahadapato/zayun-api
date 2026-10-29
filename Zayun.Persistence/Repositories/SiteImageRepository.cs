using Microsoft.EntityFrameworkCore;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ISiteImageRepository"/>.</summary>
internal sealed class SiteImageRepository(ZayunDbContext context) : Repository<SiteImage>(context), ISiteImageRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SiteImage>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().OrderBy(s => s.Slot).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<SiteImage?> GetBySlotAsync(SiteImageSlot slot, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(s => s.Slot == slot, cancellationToken);
}
