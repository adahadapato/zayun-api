using Zayun.Domain.Shared.Results;

namespace Zayun.Domain.Interfaces;

/// <summary>
/// Signing in to the admin area: login, tokens, password change. Implemented in Infrastructure using ASP.NET Core
/// Identity + JWT. Failures (wrong password, expired token…) are returned as AuthErrors, never thrown.
/// </summary>
/// <remarks>Zayun has no public sign-up: the first admin is created by the seeder.</remarks>
public interface IAuthService
{
    /// <summary>Checks email + password and, for an admin account, issues an access + refresh pair.</summary>
    /// <param name="email">The account's email.</param>
    /// <param name="password">The password.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>The tokens; or InvalidCredentials, LockedOut or NotAnAdmin.</returns>
    Task<Result<AuthTokens>> LoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>Swaps a valid refresh token for a new pair (the old one stops working).</summary>
    /// <param name="refreshToken">The refresh token the website holds.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>The new tokens; or InvalidToken.</returns>
    Task<Result<AuthTokens>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Sign out: the refresh token can't be used again. Unknown tokens are ignored.</summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="ct">Cancels the database calls.</param>
    Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Who the signed-in admin is.</summary>
    /// <param name="userId">From the access token.</param>
    /// <param name="ct">Cancels the database call.</param>
    /// <returns>The account; or UserNotFound.</returns>
    Task<Result<AdminAccount>> GetAccountAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Changes the password and signs out every other session.</summary>
    /// <param name="userId">From the access token.</param>
    /// <param name="currentPassword">The password now.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>Success; or UserNotFound, WrongPassword or WeakPassword.</returns>
    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword,
                                     CancellationToken ct = default);
}

/// <summary>A signed-in session: short-lived access token + long-lived refresh token.</summary>
/// <param name="AccessToken">The JWT to send as "Authorization: Bearer …".</param>
/// <param name="AccessTokenExpiresAt">When it stops working (UTC).</param>
/// <param name="RefreshToken">Swaps for a new pair at <c>/api/auth/refresh</c>.</param>
/// <param name="RefreshTokenExpiresAt">When it stops working (UTC).</param>
/// <param name="Account">Who is signed in.</param>
public sealed record AuthTokens(
    string AccessToken, DateTime AccessTokenExpiresAt,
    string RefreshToken, DateTime RefreshTokenExpiresAt,
    AdminAccount Account);

/// <summary>An admin account, as the admin area shows it.</summary>
/// <param name="UserId">The account's Id.</param>
/// <param name="Email">Its email.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="Roles">Its roles.</param>
public sealed record AdminAccount(
    Guid UserId, string Email, string FirstName, string LastName, IReadOnlyCollection<string> Roles);
