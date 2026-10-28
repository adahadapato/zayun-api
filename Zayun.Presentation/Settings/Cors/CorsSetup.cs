using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace Zayun.Presentation.Settings.Cors;

/// <summary>
/// Lets the Zayun website call the API from a browser (CORS).
/// <list type="number">
///   <item><description><b>Services:</b> binds and checks <see cref="CorsSettings"/>, and adds one named policy
///   (<see cref="PolicyName"/>) built from its addresses.</description></item>
///   <item><description><b>Pipeline:</b> <see cref="UseZayunCors"/> applies the policy to every request. It must come
///   before authentication, so a browser's preflight (OPTIONS) is answered without a sign-in.</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para><b>No cookies:</b> the website sends its access token in the Authorization header, so credentials aren't
/// allowed and the policy stays simple.</para>
/// <para>The addresses are read when the app starts: changing them needs a restart (a redeploy does that).</para>
/// </remarks>
public static class CorsSetup
{
    /// <summary>The policy's name.</summary>
    public const string PolicyName = "ZayunWebsite";

    /// <summary>How long a browser may remember a preflight answer before asking again.</summary>
    private static readonly TimeSpan PreflightMaxAge = TimeSpan.FromMinutes(10);

    // ───────────────────────────── 1. Services ─────────────────────────────

    /// <summary>Adds the website's CORS policy.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration (the <c>Cors</c> section).</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddZayunCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CorsSettings>()
                .Bind(configuration.GetSection(CorsSettings.SectionName))
                .Validate(settings => settings.AllowedOrigins.All(IsOrigin),
                          "Cors:AllowedOrigins must be full addresses without a path or trailing slash, " +
                          "e.g. \"https://zayun.net\".")
                .ValidateOnStart();

        services.AddCors();
        services.AddOptions<CorsOptions>()
                .Configure<IOptions<CorsSettings>>((options, settings) =>
                    options.AddPolicy(PolicyName, policy => policy
                        .WithOrigins(settings.Value.AllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetPreflightMaxAge(PreflightMaxAge)));

        return services;
    }

    // ───────────────────────────── 2. Pipeline ─────────────────────────────

    /// <summary>Applies the website's policy to every request. Call before authentication.</summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseZayunCors(this WebApplication app)
    {
        app.UseCors(PolicyName);
        return app;
    }

    // ───────────────────────────── Helpers ─────────────────────────────

    /// <summary>True for "https://host" or "http://host:port": no path, query or trailing slash.</summary>
    /// <param name="origin">An address from the settings.</param>
    private static bool IsOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
        uri.AbsolutePath == "/" && !origin.EndsWith('/') && string.IsNullOrEmpty(uri.Query);
}
