using Microsoft.IdentityModel.JsonWebTokens;

namespace Zayun.Infrastructure.Settings.Identity;

/// <summary>
/// The claim names inside Zayun's access tokens. Used by <see cref="JwtTokenGenerator"/> (writing) and by
/// Presentation's authentication setup and CurrentUser (reading), so both sides always agree.
/// </summary>
/// <remarks>Standard short JWT names. Presentation turns off ASP.NET's claim renaming (<c>MapInboundClaims = false</c>),
/// so "sub" stays "sub" instead of becoming a long XML-style URI.</remarks>
public static class JwtClaimNames
{
    /// <summary>The user's Id (AspNetUsers.Id = UserProfile.Id).</summary>
    public const string UserId = JwtRegisteredClaimNames.Sub;

    /// <summary>The user's email.</summary>
    public const string Email = JwtRegisteredClaimNames.Email;

    /// <summary>A unique Id for this token.</summary>
    public const string TokenId = JwtRegisteredClaimNames.Jti;

    /// <summary>One claim per role, e.g. "Admin" (see <c>Roles</c>).</summary>
    public const string Role = "role";
}