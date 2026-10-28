using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zayun.Domain.Interfaces;
using Zayun.Infrastructure.Services.Identity;
using Zayun.Persistence;
using Zayun.Persistence.Configurations.Identity;

namespace Zayun.Infrastructure.Settings.Identity;

/// <summary>
/// Registers sign-in: the settings, ASP.NET Core Identity (on the Zayun database), the token generator and the
/// auth service.
/// </summary>
/// <remarks>
/// No email confirmation or password-reset links exist (there's no public sign-up), so Identity's token providers
/// and data-protection keys aren't needed.
/// </remarks>
internal static class IdentitySetup
{
    /// <summary>Adds sign-in.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddZayunIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Settings
        services.AddOptions<IdentitySettings>()
                .Bind(configuration.GetSection(IdentitySettings.SectionName))
                .ValidateDataAnnotations()
                .Validate(settings => IsValid(settings.Password) && IsValid(settings.Lockout),
                          "Identity:Password or Identity:Lockout has a missing or out-of-range value.")
                .ValidateOnStart();

        services.AddOptions<JwtSettings>()
                .Bind(configuration.GetSection(JwtSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        // 2. Identity, on the same DbContext as everything else
        services.AddIdentityCore<ApplicationUser>()
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<ZayunDbContext>();

        // 3. Rules, from the settings (read when Identity first needs them)
        services.AddOptions<IdentityOptions>()
                .Configure<IOptions<IdentitySettings>>((options, settings) => Apply(options, settings.Value));

        // 4. Sign-in
        services.AddSingleton<JwtTokenGenerator>();            // stateless: settings + clock
        services.AddScoped<IAuthService, AuthService>();       // per request: shares the request's database context

        return services;
    }

    /// <summary>Copies the settings into Identity's own options.</summary>
    private static void Apply(IdentityOptions options, IdentitySettings settings)
    {
        // Sign-in is by email, so it must be unique (a rule, not a setting)
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = settings.Password.RequiredLength;
        options.Password.RequireDigit = settings.Password.RequireDigit;
        options.Password.RequireLowercase = settings.Password.RequireLowercase;
        options.Password.RequireUppercase = settings.Password.RequireUppercase;
        options.Password.RequireNonAlphanumeric = settings.Password.RequireNonAlphanumeric;
        options.Password.RequiredUniqueChars = settings.Password.RequiredUniqueChars;

        options.Lockout.MaxFailedAccessAttempts = settings.Lockout.MaxFailedAccessAttempts;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(settings.Lockout.LockoutMinutes);
        options.Lockout.AllowedForNewUsers = settings.Lockout.AllowedForNewUsers;
    }

    private static bool IsValid(object? settings) =>
        settings is not null &&
        Validator.TryValidateObject(settings, new ValidationContext(settings), null, validateAllProperties: true);
}
