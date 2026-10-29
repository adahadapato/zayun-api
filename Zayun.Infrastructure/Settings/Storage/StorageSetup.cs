using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zayun.Domain.Interfaces;

namespace Zayun.Infrastructure.Settings.Storage;

/// <summary>Registers file storage: pictures under the web root, private documents outside it.</summary>
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
                .Validate(s => IsValid(s.Local),
                          "Storage:Local needs PublicRootPath, PrivateRootPath, PrivateDownloadPath (e.g. \"/private-files\") " +
                          $"and a SigningKey of at least {LocalStorageSettings.MinSigningKeyLength} characters " +
                          "(set the key in User Secrets).")
                .ValidateOnStart();

        services.AddSingleton<LocalStoragePaths>();
        services.AddSingleton<PrivateFileLinks>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IPrivateFileStorage, LocalPrivateFileStorage>();
        return services;
    }

    private static bool IsValid(LocalStorageSettings? local) =>
        local is not null &&
        Validator.TryValidateObject(local, new ValidationContext(local), null, validateAllProperties: true);
}
