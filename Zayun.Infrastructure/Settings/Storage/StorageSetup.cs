using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zayun.Domain.Interfaces;

namespace Zayun.Infrastructure.Settings.Storage;

/// <summary>Registers file storage (a folder under the web root).</summary>
internal static class StorageSetup
{
    /// <summary>Adds file storage.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageSettings>()
                .Bind(configuration.GetSection(StorageSettings.SectionName))
                .Validate(s => !string.IsNullOrWhiteSpace(s.Local?.PublicRootPath),
                          "Storage:Local:PublicRootPath is required (e.g. \"wwwroot/uploads\").")
                .ValidateOnStart();

        services.AddSingleton<LocalStoragePaths>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        return services;
    }
}
