using Microsoft.EntityFrameworkCore;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces.Identity;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IRefreshTokenRepository"/>.</summary>
/// <remarks>Not based on <c>Repository&lt;T&gt;</c>: <see cref="RefreshToken"/> isn't a Domain entity (no soft delete,
/// no audit columns).</remarks>
internal sealed class RefreshTokenRepository(ZayunDbContext context) : IRefreshTokenRepository
{
    /// <inheritdoc />
    public void Add(RefreshToken token) => context.RefreshTokens.Add(token);

    /// <inheritdoc />
    /// <remarks>Uses the unique index on <c>TokenHash</c>; the raw token never reaches the database.</remarks>
    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var hash = RefreshToken.Hash(token);
        return context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(Guid userId, DateTime utcNow,
                                                                         CancellationToken cancellationToken = default) =>
        await context.RefreshTokens
                     .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > utcNow)
                     .ToListAsync(cancellationToken);
}