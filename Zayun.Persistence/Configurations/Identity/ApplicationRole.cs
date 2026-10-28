using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Zayun.Persistence.Configurations.Identity;

/// <summary>
/// A role (table AspNetRoles), e.g. Admin. The names come from
/// <c>Zayun.Domain.Constants.Roles</c> and are seeded at start-up; they aren't created from screens, because
/// authorization policies are tied to these exact names.
/// </summary>
/// <remarks>
/// No configuration class: <c>IdentityDbContext</c> maps Identity's tables itself, and the extra
/// <see cref="Description"/> column takes its size from the <see cref="MaxLengthAttribute"/>.
/// </remarks>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    /// <summary>Longest description (column size).</summary>
    public const int DescriptionMaxLength = 200;

    /// <summary>Required by EF Core and Identity.</summary>
    public ApplicationRole() => Id = Guid.CreateVersion7();

    /// <summary>Creates a role with the given name and an optional description.</summary>
    /// <param name="roleName">One of <c>Roles.All</c>.</param>
    /// <param name="description">What the role can do, shown on the admin screens.</param>
    public ApplicationRole(string roleName, string? description = null) : this()
    {
        Name = roleName;
        Description = description;
    }

    /// <summary>
    /// What the role can do, shown on the admin screens, e.g. "Edits the website and reads the inbox".
    /// Null when not set.
    /// </summary>
    [MaxLength(DescriptionMaxLength)]
    public string? Description { get; set; }
}