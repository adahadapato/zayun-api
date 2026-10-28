using Microsoft.Net.Http.Headers;
using Zayun.Infrastructure.Settings.Storage;

namespace Zayun.Presentation.Extensions;

/// <summary>Serves uploaded pictures from wwwroot.</summary>
public static class StorageExtensions
{
    /// <summary>A year: pictures are never changed in place (a new upload gets a new file name).</summary>
    private const string ImmutableCache = "public, max-age=31536000, immutable";

    /// <summary>Serves wwwroot as static files; uploaded pictures may be cached by browsers for a year.</summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseZayunStaticFiles(this WebApplication app)
    {
        var uploads = app.Services.GetRequiredService<LocalStoragePaths>().PublicRequestPath;

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                if (context.Context.Request.Path.StartsWithSegments(uploads, StringComparison.OrdinalIgnoreCase))
                    context.Context.Response.Headers[HeaderNames.CacheControl] = ImmutableCache;
            }
        });

        return app;
    }
}
