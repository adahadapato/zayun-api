using Zayun.Domain.Interfaces;

namespace Zayun.Application.Features.Auth.Dtos;

/// <summary>A signed-in session, as the admin area receives it.</summary>
/// <param name="AccessToken">Send as "Authorization: Bearer …".</param>
/// <param name="AccessTokenExpiresAt">When to refresh (UTC).</param>
/// <param name="RefreshToken">Swaps for a new pair at <c>/api/auth/refresh</c>.</param>
/// <param name="RefreshTokenExpiresAt">When the session ends for good (UTC).</param>
/// <param name="Account">Who is signed in.</param>
public sealed record AuthTokensDto(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    AdminAccountDto Account)
{
    /// <summary>Maps the tokens.</summary>
    /// <param name="t">The tokens.</param>
    /// <returns>The DTO.</returns>
    public static AuthTokensDto From(AuthTokens t) =>
        new(t.AccessToken, t.AccessTokenExpiresAt, t.RefreshToken, t.RefreshTokenExpiresAt,
            AdminAccountDto.From(t.Account));
}

/// <summary>The signed-in admin.</summary>
/// <param name="UserId">The account's Id.</param>
/// <param name="Email">Its email.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="Roles">Its roles.</param>
public sealed record AdminAccountDto(Guid UserId, string Email, string FirstName, string LastName,
                                     IReadOnlyCollection<string> Roles)
{
    /// <summary>Maps an account.</summary>
    /// <param name="a">The account.</param>
    /// <returns>The DTO.</returns>
    public static AdminAccountDto From(AdminAccount a) =>
        new(a.UserId, a.Email, a.FirstName, a.LastName, a.Roles);
}
