using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Zayun.Domain.Constants;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Interfaces.Identity;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;
using Zayun.Infrastructure.Settings.Identity;
using Zayun.Persistence.Configurations.Identity;

namespace Zayun.Infrastructure.Services.Identity;

/// <summary>
/// <see cref="IAuthService"/> on ASP.NET Core Identity: checks passwords (with lockout), issues and rotates tokens,
/// and changes passwords.
/// </summary>
/// <remarks>
/// <para><b>Admins only:</b> an account without the Admin role can't sign in, even with the right password.</para>
/// <para><b>Refresh tokens rotate:</b> every refresh revokes the old token. If a revoked token is used again, it was
/// copied, so every session of that account is ended.</para>
/// <para>Identity's UserManager and the repositories share one DbContext, so a single save covers both.</para>
/// </remarks>
internal sealed class AuthService(
    UserManager<ApplicationUser> users,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    JwtTokenGenerator tokenGenerator,
    TimeProvider clock,
    ILogger<AuthService> logger) : IAuthService
{
    // ───────────────────────────── 1. Sign-in ─────────────────────────────

    /// <inheritdoc />
    public async Task<Result<AuthTokens>> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(email.Trim());
        if (user is null) return AuthErrors.InvalidCredentials;

        var passwordCheck = await CheckPasswordAsync(user, password);
        if (passwordCheck.IsFailure) return passwordCheck.Error;

        var roles = await users.GetRolesAsync(user);
        if (!roles.Contains(Roles.Admin)) return AuthErrors.NotAnAdmin;

        var tokens = StartSession(user, roles, isNewSignIn: true).Tokens;
        await unitOfWork.SaveChangesAsync(ct);
        return tokens;
    }

    // ───────────────────────────── 2. Tokens ─────────────────────────────

    /// <inheritdoc />
    public async Task<Result<AuthTokens>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var now = Now;
        var stored = await refreshTokens.GetByTokenAsync(refreshToken, ct);
        if (stored is null) return AuthErrors.InvalidToken;

        // A revoked token used again: it was copied. End every session of this account.
        if (stored.RevokedAtUtc is not null)
        {
            logger.LogWarning("A revoked refresh token was used again for user {UserId}; ending all their sessions.",
                              stored.UserId);
            await RevokeAllAsync(stored.UserId, now, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return AuthErrors.InvalidToken;
        }

        if (!stored.IsActive(now)) return AuthErrors.InvalidToken;   // expired

        var user = await users.FindByIdAsync(stored.UserId.ToString());
        IList<string> roles = user is null ? [] : await users.GetRolesAsync(user);
        if (user is null || !roles.Contains(Roles.Admin))
        {
            stored.Revoke(now);
            await unitOfWork.SaveChangesAsync(ct);
            return AuthErrors.InvalidToken;
        }

        // Rotate: new pair (with the account's CURRENT roles), old token points at its replacement
        var session = StartSession(user, roles, isNewSignIn: false);
        stored.Revoke(now, session.Row.Id);
        await unitOfWork.SaveChangesAsync(ct);

        return session.Tokens;
    }

    /// <inheritdoc />
    /// <remarks>Signing out twice, or with an unknown token, still succeeds.</remarks>
    public async Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await refreshTokens.GetByTokenAsync(refreshToken, ct);
        if (stored is null) return Result.Success();

        stored.Revoke(Now);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ───────────────────────────── 3. Account ─────────────────────────────

    /// <inheritdoc />
    public async Task<Result<AdminAccount>> GetAccountAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null) return AuthErrors.UserNotFound;

        return ToAccount(user, await users.GetRolesAsync(user));
    }

    /// <inheritdoc />
    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword,
                                                  CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null) return AuthErrors.UserNotFound;

        var changed = await users.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!changed.Succeeded)
            return changed.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch))
                ? AuthErrors.WrongPassword
                : AuthErrors.WeakPassword(string.Join(" ", changed.Errors.Select(e => e.Description)));

        // Everyone signed in with the old password is signed out (including other devices)
        await RevokeAllAsync(user.Id, Now, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ───────────────────────────── Helpers ─────────────────────────────

    /// <summary>The current time (UTC), from the app's clock.</summary>
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    /// <summary>Checks the password, counting failures towards the lockout.</summary>
    private async Task<Result> CheckPasswordAsync(ApplicationUser user, string password)
    {
        if (await users.IsLockedOutAsync(user)) return AuthErrors.LockedOut;

        if (await users.CheckPasswordAsync(user, password))
        {
            if (user.AccessFailedCount > 0) await users.ResetAccessFailedCountAsync(user);
            return Result.Success();
        }

        await users.AccessFailedAsync(user);
        return await users.IsLockedOutAsync(user) ? AuthErrors.LockedOut : AuthErrors.InvalidCredentials;
    }

    /// <summary>
    /// Makes a new access + refresh pair and adds the refresh token for saving. Does NOT save: the caller may have
    /// more to change first (e.g. revoking the old token).
    /// </summary>
    /// <param name="user">The account signing in.</param>
    /// <param name="roles">Its roles.</param>
    /// <param name="isNewSignIn">True for a password sign-in (updates <c>LastLoginAt</c>); false for a refresh.</param>
    private Session StartSession(ApplicationUser user, IList<string> roles, bool isNewSignIn)
    {
        var access = tokenGenerator.CreateAccessToken(user, roles);
        var refresh = tokenGenerator.CreateRefreshToken();

        var row = new RefreshToken(user.Id, refresh.Value, refresh.IssuedAtUtc, refresh.ExpiresAtUtc);
        refreshTokens.Add(row);

        if (isNewSignIn) user.LastLoginAt = access.IssuedAtUtc;

        var tokens = new AuthTokens(access.Value, access.ExpiresAtUtc, refresh.Value, refresh.ExpiresAtUtc,
                                    ToAccount(user, roles));
        return new Session(tokens, row);
    }

    /// <summary>Revokes every working refresh token of an account (not saved).</summary>
    private async Task RevokeAllAsync(Guid userId, DateTime now, CancellationToken ct)
    {
        foreach (var token in await refreshTokens.GetActiveForUserAsync(userId, now, ct))
            token.Revoke(now);
    }

    private static AdminAccount ToAccount(ApplicationUser user, IList<string> roles) =>
        new(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, roles.ToArray());

    /// <summary>A new session: the tokens to hand out and the refresh-token row to save.</summary>
    private sealed record Session(AuthTokens Tokens, RefreshToken Row);
}
