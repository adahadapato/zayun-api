using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Zayun.Domain.Constants;
using Zayun.Persistence.Seeding;

namespace Zayun.Persistence.Configurations.Identity;

/// <summary>
/// Creates the first Admin from the <c>SeedData</c> settings: the login account and the Admin role, together.
/// </summary>
/// <remarks>
/// <para><b>Runs once:</b> if a user with that email already exists, nothing is created. The seeder only makes sure
/// that user still has the Admin role.</para>
/// <para><b>Missing settings:</b> admin seeding is skipped with a warning; start-up continues.</para>
/// <para><b>Weak password:</b> start-up stops with Identity's reasons, because a half-made admin is worse than none.</para>
/// </remarks>
internal static class ApplicationUserSeeder
{
    /// <summary>Creates the first Admin if it doesn't exist yet.</summary>
    /// <param name="userManager">Identity's user manager (same DbContext as <paramref name="context"/>).</param>
    /// <param name="context">The database, for the transaction.</param>
    /// <param name="settings">The <c>SeedData</c> section.</param>
    /// <param name="logger">Start-up log.</param>
    /// <param name="cancellationToken">Stops seeding.</param>
    /// <exception cref="InvalidOperationException">Identity refused the account (e.g. the password breaks the rules).</exception>
    public static async Task SeedAsync(UserManager<ApplicationUser> userManager, ZayunDbContext context,
                                       SeedDataSettings settings, ILogger logger,
                                       CancellationToken cancellationToken = default)
    {
        // 1. Settings present?
        if (!settings.CanSeedAdmin)
        {
            logger.LogWarning(
                "Admin seeding skipped: SeedData:AdminEmail, AdminPassword, AdminFirstName and AdminLastName must " +
                "all be set.");
            return;
        }

        var email = settings.AdminEmail.Trim();

        // 2. Already there? Only make sure it's still an Admin
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, Roles.Admin))
            {
                IdentityResultCheck.Ensure(await userManager.AddToRoleAsync(existing, Roles.Admin),
                                           $"give '{email}' the Admin role");
                logger.LogInformation("Gave existing user {Email} the Admin role", email);
            }
            return;
        }

        // 3. Create account + role together, or nothing
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,     // the person running the deployment owns this address
            FirstName = settings.AdminFirstName.Trim(),
            LastName = settings.AdminLastName.Trim()
        };

        IdentityResultCheck.Ensure(await userManager.CreateAsync(user, settings.AdminPassword),
                                   $"create admin '{email}'");
        IdentityResultCheck.Ensure(await userManager.AddToRoleAsync(user, Roles.Admin),
                                   $"give '{email}' the Admin role");

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Seeded admin {Email}", email);
    }
}

/// <summary>Turns a failed <see cref="IdentityResult"/> into a clear start-up error.</summary>
internal static class IdentityResultCheck
{
    /// <summary>Throws if <paramref name="result"/> failed, listing Identity's reasons.</summary>
    /// <param name="result">What Identity returned.</param>
    /// <param name="action">What was being done, for the message (e.g. "create role 'Admin'").</param>
    /// <exception cref="InvalidOperationException">The result failed.</exception>
    public static void Ensure(IdentityResult result, string action)
    {
        if (result.Succeeded) return;

        var reasons = string.Join("; ", result.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Seeding failed to {action}: {reasons}");
    }
}
