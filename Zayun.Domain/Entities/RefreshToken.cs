using System.Security.Cryptography;
using System.Text;

namespace Zayun.Domain.Entities;

/// <summary>
/// One refresh token handed to an app (table RefreshTokens). Only a SHA-256 HASH of the token is stored, so a leaked
/// database can't be used to sign in.
/// </summary>
/// <remarks>
/// <para><b>Rotation:</b> every refresh revokes the old token and issues a new one (<see cref="ReplacedByTokenId"/> links
/// them). If a revoked token is ever presented again, someone copied it: AuthService then revokes every token of that
/// user.</para>
/// <para><b>Not based on <see cref="Entity{TId}"/>:</b> no soft delete or audit columns. Tokens are revoked, then
/// cleaned up once expired.</para>
/// <para>Uses only .NET's own hashing; nothing from ASP.NET Identity.</para>
/// </remarks>
public sealed class RefreshToken
{
    /// <summary>Length of a SHA-256 hash written as hex.</summary>
    public const int TokenHashLength = 64;

    /// <summary>Required by EF Core.</summary>
    private RefreshToken() { }

    /// <summary>Records a newly issued token.</summary>
    /// <param name="userId">The user it signs in.</param>
    /// <param name="token">The raw token (only its hash is kept).</param>
    /// <param name="createdAtUtc">When it was issued.</param>
    /// <param name="expiresAtUtc">When it stops working.</param>
    /// <exception cref="ArgumentException">A blank token.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Expiry not after creation.</exception>
    public RefreshToken(Guid userId, string token, DateTime createdAtUtc, DateTime expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresAtUtc, createdAtUtc);

        Id = Guid.CreateVersion7();
        UserId = userId;
        TokenHash = Hash(token);
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>Row Id.</summary>
    public Guid Id { get; private set; }

    /// <summary>The user this token signs in.</summary>
    public Guid UserId { get; private set; }

    /// <summary>SHA-256 of the token as upper-case hex. The raw token is never stored.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>When it was issued (UTC).</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>When it stops working (UTC).</summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>When it was revoked (sign-out, rotation, password change, deactivation); null while usable.</summary>
    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>The token that replaced this one at rotation; null if none.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    /// <summary>True while it can still be exchanged for new tokens.</summary>
    /// <param name="utcNow">The current time.</param>
    public bool IsActive(DateTime utcNow) => RevokedAtUtc is null && utcNow < ExpiresAtUtc;

    /// <summary>Stops this token working. Revoking twice keeps the first time.</summary>
    /// <param name="utcNow">The current time.</param>
    /// <param name="replacedByTokenId">The new token, when revoked by rotation.</param>
    public void Revoke(DateTime utcNow, Guid? replacedByTokenId = null)
    {
        if (RevokedAtUtc is not null) return;
        RevokedAtUtc = utcNow;
        ReplacedByTokenId = replacedByTokenId;
    }

    /// <summary>SHA-256 of a raw token as upper-case hex: how tokens are looked up.</summary>
    /// <param name="token">The raw token.</param>
    /// <returns>The 64-character hash.</returns>
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}