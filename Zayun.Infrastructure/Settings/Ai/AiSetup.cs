using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zayun.Domain.Interfaces;

namespace Zayun.Infrastructure.Settings.Ai;

/// <summary>Registers the AI helpers (the tag check and the quote helper): Claude when <c>Ai:Enabled</c> is true and a key
/// is set, otherwise stand-ins that are never asked.</summary>
/// <remarks>All helpers share one connection to Claude (<see cref="ClaudeMessages"/>), so they use the same settings,
/// timeout and log lines. A new helper only adds its own class and one line here.</remarks>
internal static class AiSetup
{
    /// <summary>Adds the AI helpers.</summary>
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
            // The one shared connection to Claude.
            services.AddHttpClient<ClaudeMessages>(client =>
            {
                client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds + 5);
            });

            // The helpers that use it.
            services.AddTransient<ITagAdvisor, ClaudeTagAdvisor>();
            services.AddTransient<IQuoteAdvisor, ClaudeQuoteAdvisor>();
            services.AddTransient<IPostAdvisor, ClaudePostAdvisor>();
        }
        else
        {
            services.AddSingleton<ITagAdvisor, NoTagAdvisor>();
            services.AddSingleton<IQuoteAdvisor, NoQuoteAdvisor>();
            services.AddSingleton<IPostAdvisor, NoPostAdvisor>();
        }

        return services;
    }
}
