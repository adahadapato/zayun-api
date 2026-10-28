using Microsoft.Extensions.Options;
using Zayun.Domain.Constants;

namespace Zayun.Presentation.Settings.OpenApi;

/// <summary>
/// The OpenAPI document (built into ASP.NET Core) and the Swagger page to try the API. Both only when
/// <c>OpenApi:Enabled</c> is true (Development).
/// </summary>
public static class OpenApiSetup
{
    // ───────────────────────────── 1. Services ─────────────────────────────

    /// <summary>Adds the OpenAPI document generator.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration (the <c>OpenApi</c> section).</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddZayunOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(AppSettings.OpenApi.Section);
        services.AddOptions<OpenApiSettings>().Bind(section);

        // The document name is needed now, at registration
        var settings = section.Get<OpenApiSettings>() ?? new OpenApiSettings();
        var documentName = string.IsNullOrWhiteSpace(settings.Version) ? "v1" : settings.Version;

        services.AddOpenApi(documentName, options =>
        {
            options.AddDocumentTransformer<DocumentInfoTransformer>();      // title, version, contact
            options.AddDocumentTransformer<BearerSecurityTransformer>();    // "Authorize" button
            options.AddOperationTransformer<BearerSecurityTransformer>();   // lock icons
        });

        return services;
    }

    // ───────────────────────────── 2. Pipeline ─────────────────────────────

    /// <summary>Serves the document and the Swagger page, when switched on.</summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseZayunOpenApi(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<IOptions<OpenApiSettings>>().Value;
        if (!settings.Enabled) return app;

        // 2a. The JSON document, e.g. /openapi/v1.json
        app.MapOpenApi(settings.DocumentPath);

        // 2b. The Swagger page, e.g. /swagger
        app.UseSwaggerUI(ui =>
        {
            ui.RoutePrefix = settings.UiPath.Trim('/');
            ui.DocumentTitle = settings.Title;
            ui.SwaggerEndpoint(settings.DocumentUrl, $"{settings.Title} {settings.Version}");
        });

        // 2c. The API's root opens Swagger, so a browser visit lands somewhere useful
        app.MapGet("/", () => Results.Redirect($"/{settings.UiPath.Trim('/')}")).ExcludeFromDescription();

        return app;
    }
}
