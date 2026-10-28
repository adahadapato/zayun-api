using ImageMagick;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zayun.Domain.Interfaces;
using Zayun.Domain.ValueObjects;

namespace Zayun.Infrastructure.Settings.Images;

/// <summary>Registers the picture pipeline (Magick.NET) with process-wide safety limits.</summary>
internal static class ImageSetup
{
    /// <summary>Most memory Magick.NET may use for one picture.</summary>
    private const ulong MaxMemoryBytes = 256UL * 1024 * 1024;

    /// <summary>Adds the picture pipeline.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddImages(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ImageSettings>()
                .Bind(configuration.GetSection(ImageSettings.SectionName))
                .ValidateDataAnnotations()
                .Validate(s => s.ThumbnailMaxSide <= s.MaxSide, "Images:ThumbnailMaxSide can't be bigger than Images:MaxSide.")
                .ValidateOnStart();

        // Anything bigger is refused before it can use the server's memory
        ResourceLimits.Width = ImageDimensions.MaxSide;
        ResourceLimits.Height = ImageDimensions.MaxSide;
        ResourceLimits.Memory = MaxMemoryBytes;

        services.AddSingleton<IImageProcessor, MagickImageProcessor>();
        return services;
    }
}
