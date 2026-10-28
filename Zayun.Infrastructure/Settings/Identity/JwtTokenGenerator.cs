using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Zayun.Persistence.Configurations.Identity;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Zayun.Infrastructure.Settings.Identity;

/// <summary>
/// Makes the two tokens of a signed-in session:
/// <list type="number">
///   <item><description><b>Access token:</b> a signed JWT (HS256) with the user's Id, email and roles; short-lived
///   (<see cref="JwtSettings.ExpiryMinutes"/>).</description></item>
///   <item><description><b>Refresh token:</b> 64 random bytes, URL-safe; long-lived
///   (<see cref="JwtSettings.RefreshTokenDays"/>). Only its hash is ever stored.</description></item>
/// </list>
/// </summary>
/// <remarks>Makes tokens only; storing, rotating and revoking refresh tokens is <c>AuthService</c>'s job.</remarks>
internal sealed class JwtTokenGenerator(IOptions<JwtSettings> options, TimeProvider clock)
{
    /// <summary>Refresh-token size: 64 bytes = 512 bits, far beyond guessing.</summary>
    private const int RefreshTokenBytes = 64;

    /// <summary>Thread-safe; one instance for the whole app.</summary>
    private static readonly JsonWebTokenHandler Handler = new();

    // ───────────────────────────── 1. Access token ─────────────────────────────

    /// <summary>Creates a signed access token for a user.</summary>
    /// <param name="user">The signed-in user.</param>
    /// <param name="roles">The user's roles (one claim each).</param>
    /// <returns>The token and when it expires.</returns>
    public IssuedToken CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(settings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtClaimNames.UserId, user.Id.ToString()),
            new(JwtClaimNames.TokenId, Guid.NewGuid().ToString())
        };
        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(JwtClaimNames.Email, user.Email));
        claims.AddRange(roles.Select(role => new Claim(JwtClaimNames.Role, role)));

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(settings.CreateSigningKey(), SecurityAlgorithms.HmacSha256)
        });

        return new IssuedToken(token, now, expires);
    }

    // ───────────────────────────── 2. Refresh token ─────────────────────────────

    /// <summary>Creates a new random refresh token (not yet stored).</summary>
    /// <returns>The raw token (give it to the app once; store only its hash) and its lifetime.</returns>
    public IssuedToken CreateRefreshToken()
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        return new IssuedToken(value, now, now.AddDays(options.Value.RefreshTokenDays));
    }
}

/// <summary>A token as handed to the app, with its lifetime.</summary>
/// <param name="Value">The token itself.</param>
/// <param name="IssuedAtUtc">When it was made.</param>
/// <param name="ExpiresAtUtc">When it stops working.</param>
internal sealed record IssuedToken(string Value, DateTime IssuedAtUtc, DateTime ExpiresAtUtc);