using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zayun.Infrastructure.Settings.Application;

/// <summary>Registers <see cref="ApplicationSettings"/>. Called from <see cref="DependencyInjection.AddInfrastructure"/>.</summary>
internal static class ApplicationSetup
{
    /// <summary>Binds and checks the "AppSettings" section when the app starts.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddApplicationSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApplicationSettings>()
                .Bind(configuration.GetSection(ApplicationSettings.SectionName))
                .ValidateDataAnnotations()
                .Validate(s => !s.ApiBaseUrl.EndsWith('/') && !s.WebsiteBaseUrl.EndsWith('/'),
                          "AppSettings:ApiBaseUrl and AppSettings:WebsiteBaseUrl must not end with '/'.")
                .ValidateOnStart();

        return services;
    }
}