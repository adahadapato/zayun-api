using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Zayun.Persistence.Configurations.Identity;

/// <summary>
/// An admin's login account (table AspNetUsers): email, password hash, lockout, name and when they last signed in.
/// Managed by ASP.NET Core Identity's <c>UserManager</c>.
/// </summary>
/// <remarks>
/// <para>Zayun has no public accounts, so the few details an admin has live here rather than in a separate Domain
/// profile.</para>
/// <para>Ids are version-7 Guids (time-ordered), like every Domain Id.</para>
/// <para>No configuration class: <c>IdentityDbContext</c> maps Identity's tables itself, and the extra columns take
/// their sizes from the <see cref="MaxLengthAttribute"/>s.</para>
/// </remarks>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Longest first or last name (column size).</summary>
    public const int NameMaxLength = 100;

    /// <summary>Creates a user with a new time-ordered Id.</summary>
    public ApplicationUser() => Id = Guid.CreateVersion7();

    /// <summary>First name, shown in the admin area.</summary>
    [MaxLength(NameMaxLength)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Last name, shown in the admin area.</summary>
    [MaxLength(NameMaxLength)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>When the user last signed in successfully (UTC); null if never.</summary>
    public DateTime? LastLoginAt { get; set; }
}
