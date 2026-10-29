using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zayun.Domain.Interfaces;
using Zayun.Persistence.Configurations.Identity;

namespace Zayun.Persistence.Seeding;

/// <summary>
/// Applies database migrations and seeds the data the site needs, at start-up:
/// <list type="number">
///   <item><description>Pending migrations.</description></item>
///   <item><description>Roles.</description></item>
///   <item><description>The first admin (from <c>SeedData</c>).</description></item>
///   <item><description>The starter content (only into empty tables).</description></item>
///   <item><description>The shared tag list (only while it's empty), tidying existing tag names.</description></item>
/// </list>
/// </summary>
/// <remarks>Any failure is logged and stops start-up: a half-ready database is worse than an API that won't start.</remarks>
public static class DbSeeder
{
    /// <summary>Migrates and seeds the database.</summary>
    /// <param name="services">The app's root service provider.</param>
    /// <param name="cancellationToken">Stops start-up.</param>
    public static async Task SeedDatabaseAsync(this IServiceProvider services,
                                               CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));
        var context = provider.GetRequiredService<ZayunDbContext>();
        var settings = provider.GetRequiredService<IOptions<SeedDataSettings>>().Value;

        await RunStepAsync(logger, "1: migrations",
            () => context.Database.MigrateAsync(cancellationToken));

        await RunStepAsync(logger, "2: roles",
            () => ApplicationRoleSeeder.SeedAsync(provider.GetRequiredService<RoleManager<ApplicationRole>>(),
                                                  logger, cancellationToken));

        await RunStepAsync(logger, "3: first admin",
            () => ApplicationUserSeeder.SeedAsync(provider.GetRequiredService<UserManager<ApplicationUser>>(),
                                                  context, settings, logger, cancellationToken));

        await RunStepAsync(logger, "4: starter content",
            () => StarterDataSeeder.SeedAsync(context, provider.GetRequiredService<IFileStorage>(),
                                              provider.GetRequiredService<IImageProcessor>(), settings, logger,
                                              cancellationToken));

        await RunStepAsync(logger, "5: tag list",
            () => TagListSeeder.SeedAsync(context, logger, cancellationToken));

        logger.LogInformation("Database initialization completed.");
    }

    /// <summary>Runs one step, logging its start, end or failure.</summary>
    private static async Task RunStepAsync(ILogger logger, string step, Func<Task> run)
    {
        logger.LogInformation("Database initialization step {Step}: starting.", step);
        try
        {
            await run();
            logger.LogInformation("Database initialization step {Step}: done.", step);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database initialization step {Step} FAILED. Start-up cannot continue.", step);
            throw;
        }
    }
}
