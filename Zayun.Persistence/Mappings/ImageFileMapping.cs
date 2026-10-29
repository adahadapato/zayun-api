using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.ValueObjects;

namespace Zayun.Persistence.Mappings;

/// <summary>
/// Maps an <see cref="ImageFile"/> (and its <see cref="ImageDimensions"/>) as columns inside its owner's table,
/// e.g. <c>Image_Path</c>, <c>Image_Dimensions_Width</c>. Used by ProjectImage, Presentation, Award and SiteImage.
/// </summary>
/// <remarks>
/// <para>A reusable mapping helper, not a table configuration: table configurations call it for their image columns.</para>
/// <para>When the owner's image is optional (e.g. a presentation with no poster), EF Core makes these columns nullable
/// automatically and reads "all null" back as no image.</para>
/// </remarks>
public static class ImageFileMapping
{
    /// <summary>Maps the image's columns.</summary>
    /// <typeparam name="TOwner">The entity that owns the image.</typeparam>
    /// <param name="image">EF Core's builder for the owned image.</param>
    public static void MapImageFile<TOwner>(this OwnedNavigationBuilder<TOwner, ImageFile> image)
        where TOwner : class
    {
        image.Property(i => i.Path).IsRequired().HasMaxLength(ImageFile.PathMaxLength);
        image.Property(i => i.ThumbnailPath).HasMaxLength(ImageFile.PathMaxLength);
        image.Property(i => i.ContentType).IsRequired().HasMaxLength(ImageFile.ContentTypeMaxLength);
        image.Property(i => i.FileSizeBytes).IsRequired();
        image.Property(i => i.Hash).IsRequired().HasMaxLength(ImageFile.HashMaxLength);

        image.OwnsOne(i => i.Dimensions, size =>
        {
            size.Property(d => d.Width).IsRequired();
            size.Property(d => d.Height).IsRequired();
        });
        image.Navigation(i => i.Dimensions).IsRequired();
    }
}