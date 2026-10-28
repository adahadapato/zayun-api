using System.Text.Json.Serialization;
using Zayun.Infrastructure;
using Zayun.Presentation.Middleware;
using Zayun.Presentation.Settings.Authentication;
using Zayun.Presentation.Settings.Cors;
using Zayun.Presentation.Settings.OpenApi;
using Zayun.Presentation.Settings.RateLimiting;

namespace Zayun.Presentation;

/// <summary>
/// The composition root: every layer, then what's specific to hosting the API (OpenAPI, sign-in, CORS, rate
/// limits, errors, JSON).
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds everything the API needs.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Layers: Infrastructure composes Application and Persistence (once each)
        services.AddInfrastructure(configuration);

        // 2. API
        services.AddZayunOpenApi(configuration);          // 2a. OpenAPI document + Swagger page
        services.AddZayunAuthentication();                // 2b. JWT bearer, the Admin policy, CurrentUser
        services.AddZayunCors(configuration);             //     the website may call the API from a browser
        services.AddZayunRateLimiting();                  //     limits on public endpoints (Contact, sign-in)

        // 2c. Errors: one ProblemDetails shape for thrown exceptions AND Result failures
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // 2d. JSON: enums as names ("Featured", not 1), both in and out; numbers are still accepted
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        return services;
    }
}
