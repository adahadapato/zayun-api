using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zayun.Worker;

/// <summary>
/// Reserved for scheduled jobs (e.g. tidying old refresh tokens, or a nightly sitemap). Nothing runs yet; the call in
/// Program.cs is already in place so adding a job means only registering it here.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds the background jobs (none yet).</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddWorker(this IServiceCollection services, IConfiguration configuration)
    {
        // e.g. services.AddHostedService<RefreshTokenCleanupService>();
        return services;
    }
}
