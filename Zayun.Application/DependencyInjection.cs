using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Zayun.Application.Common.Behaviours;

namespace Zayun.Application;

/// <summary>
/// Registers the Application layer: the clock, the cache, the validators and the MediatR pipeline.
/// </summary>
/// <remarks>Uses only DI abstractions, so Application stays free of ASP.NET Core. Called by Infrastructure.</remarks>
public static class DependencyInjection
{
    /// <summary>Adds the Application layer's services.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // 1. The clock: .NET's built-in TimeProvider (FakeTimeProvider in tests)
        services.AddSingleton(TimeProvider.System);

        // 2. Cache: in-memory by default; a shared cache (e.g. Redis) can be added later without code changes here
        services.AddHybridCache();

        // 3. Validators: without this, ValidationBehavior finds none and NO validation runs
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // 4. MediatR: outermost first: Logging → Validation → Caching → Cache invalidation → handler
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
            // v13+ only: cfg.LicenseKey = "...";
        });

        return services;
    }
}
