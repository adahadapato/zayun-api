using Microsoft.EntityFrameworkCore;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ICoreExpertiseRepository"/>.</summary>
internal sealed class CoreExpertiseRepository(ZayunDbContext context)
    : ContentRepository<CoreExpertise>(context), ICoreExpertiseRepository
{
    /// <inheritdoc />
    /// <remarks>SQL Server's default collation compares without case, so "explainable ai" matches "Explainable AI".</remarks>
    public Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken = default)
    {
        var clean = name.Trim();
        return Set.AnyAsync(c => c.Name == clean && (exceptId == null || c.Id != exceptId), cancellationToken);
    }
}
