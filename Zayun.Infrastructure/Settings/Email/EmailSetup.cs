using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zayun.Domain.Constants;
using Zayun.Domain.Interfaces;

namespace Zayun.Infrastructure.Settings.Email;

/// <summary>Registers email: the settings, and SMTP or log-only sending depending on <c>Email:Enabled</c>.</summary>
internal static class EmailSetup
{
    /// <summary>Adds email.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailSettings>()
                .Bind(configuration.GetSection(EmailSettings.SectionName))
                .ValidateDataAnnotations()
                .Validate(s => !s.Enabled || !string.IsNullOrWhiteSpace(s.Host),
                          "Email:Host is required when Email:Enabled is true.")
                .ValidateOnStart();

        if (configuration.GetValue<bool>(AppSettings.Email.Enabled))
            services.AddTransient<IEmailSender, SmtpEmailSender>();
        else
            services.AddTransient<IEmailSender, LogOnlyEmailSender>();

        return services;
    }
}
