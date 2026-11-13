using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zayun.Domain.Interfaces;

namespace Zayun.Infrastructure.Settings.Ai;

/// <summary>Registers the AI helpers (the tag check and the quote helper): Claude when <c>Ai:Enabled</c> is true and a key
/// is set, otherwise stand-ins that are never asked.</summary>
internal static class AiSetup
{
    /// <summary>Adds the AI tag check.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AiSettings>()
                .Bind(configuration.GetSection(AiSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        var settings = configuration.GetSection(AiSettings.SectionName).Get<AiSettings>() ?? new AiSettings();
        if (settings.IsUsable)
        {
            services.AddHttpClient<ITagAdvisor, ClaudeTagAdvisor>(client =>
            {
                client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds + 5);
            });
            services.AddHttpClient<IQuoteAdvisor, ClaudeQuoteAdvisor>(client =>
            {
                client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds + 5);
            });
        }
        else
        {
            services.AddSingleton<ITagAdvisor, NoTagAdvisor>();
            services.AddSingleton<IQuoteAdvisor, NoQuoteAdvisor>();
        }

        return services;
    }
}
