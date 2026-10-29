using Microsoft.EntityFrameworkCore;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IPersonalDetailsRepository"/>.</summary>
internal sealed class PersonalDetailsRepository(ZayunDbContext context)
    : Repository<PersonalDetails>(context), IPersonalDetailsRepository
{
    /// <inheritdoc />
    public Task<PersonalDetails?> GetAsync(CancellationToken cancellationToken = default) =>
        Set.OrderBy(p => p.CreatedAt).FirstOrDefaultAsync(cancellationToken);
}
