using System.ComponentModel.DataAnnotations;
using Zayun.Domain.Constants;

namespace Zayun.Infrastructure.Settings.Identity;

/// <summary>
/// The <c>"Identity:Lockout"</c> sub-section (keys in <see cref="AppSettings.Identity.Lockout"/>): a temporary
/// lockout after too many wrong passwords.
/// </summary>
/// <remarks>Different from an admin deactivating an account (<c>ApplicationUser.IsActive</c>): a lockout ends by
/// itself after <see cref="LockoutMinutes"/>.</remarks>
public sealed class LockoutSettings
{
    /// <summary>Wrong passwords allowed before the lockout (1–100).</summary>
    [Range(1, 100)]
    public int MaxFailedAccessAttempts { get; set; } = 5;

    /// <summary>How long the lockout lasts, in minutes (1–1440, i.e. up to a day).</summary>
    [Range(1, 1440)]
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>Whether new accounts can be locked out at all.</summary>
    public bool AllowedForNewUsers { get; set; } = true;
}