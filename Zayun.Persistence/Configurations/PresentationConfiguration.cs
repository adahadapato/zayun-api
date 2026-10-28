using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;
using Zayun.Persistence.Mappings;

namespace Zayun.Persistence.Configurations;

/// <summary>
/// Table <c>Presentations</c>, with its optional picture stored as columns (<c>Image_Path</c>…), all null when there
/// is none.
/// </summary>
public sealed class PresentationConfiguration : ContentEntityConfiguration<Presentation>
{
    /// <inheritdoc />
    protected override void ConfigureContent(EntityTypeBuilder<Presentation> builder)
    {
        builder.ToTable("Presentations");

        builder.Property(p => p.Title).IsRequired().HasMaxLength(Presentation.TitleMaxLength);
        builder.Property(p => p.EventName).IsRequired().HasMaxLength(Presentation.EventMaxLength);
        builder.Property(p => p.Year).IsRequired();
        builder.Property(p => p.Type).IsRequired();
        builder.Property(p => p.ImageAlt).HasMaxLength(Presentation.ImageAltMaxLength);

        builder.OwnsOne(p => p.Image, image => image.MapImageFile());
    }
}
