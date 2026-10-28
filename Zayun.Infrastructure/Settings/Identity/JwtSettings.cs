using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Zayun.Domain.Constants;

namespace Zayun.Infrastructure.Settings.Identity;

/// <summary>
/// The <c>"Jwt"</c> section (keys in <see cref="AppSettings.Jwt"/>): how access and refresh tokens are made.
/// </summary>
/// <remarks><see cref="Key"/> is a SECRET: User Secrets in development, the CI pipeline in production.</remarks>
public sealed class JwtSettings
{
    /// <summary>The section's name in appsettings.json.</summary>
    public const string SectionName = AppSettings.Jwt.Section;

    /// <summary>Shortest key accepted (HMAC-SHA256 needs at least 256 bits).</summary>
    public const int MinimumKeyLength = 32;

    /// <summary>The signing key.</summary>
    [Required(ErrorMessage = "Jwt:Key is empty. Set it in User Secrets (development) or as Jwt__Key (production).")]
    [MinLength(MinimumKeyLength, ErrorMessage = "Jwt:Key must be at least 32 characters.")]
    public string Key { get; set; } = string.Empty;

    /// <summary>Who issues tokens (this API).</summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Who tokens are for (the admin area).</summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>How long an access token works.</summary>
    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 15;

    /// <summary>How long a refresh token works (the admin stays signed in this long without using the site).</summary>
    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 14;

    /// <summary>The key, ready for signing and checking tokens.</summary>
    /// <returns>The key.</returns>
    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(Key));
}
