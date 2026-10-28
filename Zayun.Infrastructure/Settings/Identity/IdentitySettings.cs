using System.ComponentModel.DataAnnotations;
using Zayun.Domain.Constants;

namespace Zayun.Infrastructure.Settings.Identity;

/// <summary>
/// The <c>"Identity"</c> section (keys in <see cref="AppSettings.Identity"/>): password and lockout rules for admin
/// accounts.
/// </summary>
public sealed class IdentitySettings
{
    /// <summary>The section's name in appsettings.json.</summary>
    public const string SectionName = AppSettings.Identity.Section;

    /// <summary>How strong a password must be.</summary>
    [Required]
    public PasswordSettings Password { get; set; } = new();

    /// <summary>What happens after too many wrong passwords.</summary>
    [Required]
    public LockoutSettings Lockout { get; set; } = new();
}
