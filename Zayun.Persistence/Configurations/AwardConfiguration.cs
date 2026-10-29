using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;
using Zayun.Persistence.Mappings;

namespace Zayun.Persistence.Configurations;

/// <summary>
/// Table <c>Awards</c>, with its optional picture stored as columns (<c>Image_Path</c>…), all null when there is none.
/// </summary>
public sealed class AwardConfiguration : ContentEntityConfiguration<Award>
{
    /// <inheritdoc />
    protected override void ConfigureContent(EntityTypeBuilder<Award> builder)
    {
        builder.ToTable("Awards");

        builder.Property(a => a.Title).IsRequired().HasMaxLength(Award.TitleMaxLength);
        builder.Property(a => a.AwardedBy).IsRequired().HasMaxLength(Award.AwardedByMaxLength);
        builder.Property(a => a.Year).IsRequired();
        builder.Property(a => a.Description).IsRequired().HasMaxLength(Award.DescriptionMaxLength);
        builder.Property(a => a.Url).HasMaxLength(Award.UrlMaxLength);
        builder.Property(a => a.IsFeatured).IsRequired();
        builder.Property(a => a.ImageAlt).HasMaxLength(Award.ImageAltMaxLength);

        builder.OwnsOne(a => a.Image, image => image.MapImageFile());
    }
}
