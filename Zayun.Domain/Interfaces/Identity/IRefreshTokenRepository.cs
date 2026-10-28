using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces.Identity;

/// <summary>
/// Loads and saves <see cref="RefreshToken"/>s (table RefreshTokens). Used only by the Identity services in
/// Infrastructure.
/// </summary>
/// <remarks>
/// <para>Lives in Persistence, not Domain: <see cref="RefreshToken"/> is a sign-in mechanism the Domain knows nothing
/// about.</para>
/// <para>Like every repository, it never saves: the caller saves once (UserManager's own save, or
/// <c>IUnitOfWork.SaveChangesAsync</c>).</para>
/// </remarks>
public interface IRefreshTokenRepository
{
    /// <summary>Marks a newly issued token to be inserted on the next save.</summary>
    /// <param name="token">The new token.</param>
    void Add(RefreshToken token);

    /// <summary>Finds a token by its raw value (hashed here, then looked up). Tracked, so it can be revoked.</summary>
    /// <param name="token">The raw token the app sent.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The token, or null if there is none (it may still be revoked or expired: check <c>IsActive</c>).</returns>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every token of a user that still works. Tracked: used to revoke them all (sign out everywhere, password
    /// change, deactivation, or a stolen token being re-used).
    /// </summary>
    /// <param name="userId">The user.</param>
    /// <param name="utcNow">The current time (expired tokens are left out).</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(Guid userId, DateTime utcNow,
                                                            CancellationToken cancellationToken = default);
}