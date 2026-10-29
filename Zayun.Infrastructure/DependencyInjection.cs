using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zayun.Application;
using Zayun.Infrastructure.Settings.Ai;
using Zayun.Infrastructure.Settings.Application;
using Zayun.Infrastructure.Settings.Email;
using Zayun.Infrastructure.Settings.Identity;
using Zayun.Infrastructure.Settings.Images;
using Zayun.Infrastructure.Settings.Storage;
using Zayun.Persistence;

namespace Zayun.Infrastructure;

/// <summary>
/// Registers Application, Persistence and everything Infrastructure provides: settings, email, file storage, the
/// picture pipeline, sign-in and the AI tag check.
/// </summary>
/// <remarks>Called once from Presentation's DependencyInjection.</remarks>
public static class DependencyInjection
{
    /// <summary>Adds the Infrastructure layer (and the layers it composes).</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();                   // MediatR pipeline, validators, caching, TimeProvider
        services.AddPersistence(configuration);      // SQL Server DbContext, unit of work, repositories

        services.AddApplicationSettings(configuration);
        services.AddEmail(configuration);
        services.AddStorage(configuration);
        services.AddImages(configuration);
        services.AddZayunIdentity(configuration);
        services.AddAi(configuration);             // the AI tag check (Claude), or the built-in rules only

        return services;
    }
}
