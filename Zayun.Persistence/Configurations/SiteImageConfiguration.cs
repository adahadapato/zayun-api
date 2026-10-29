using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;
using Zayun.Persistence.Mappings;

namespace Zayun.Persistence.Configurations;

/// <summary>Table <c>SiteImages</c>: one picture per named place (e.g. the owner's photo), slot unique.</summary>
public sealed class SiteImageConfiguration : EntityConfiguration<SiteImage>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<SiteImage> builder)
    {
        builder.ToTable("SiteImages");

        builder.Property(s => s.Slot).IsRequired();
        builder.Property(s => s.AltText).IsRequired().HasMaxLength(SiteImage.AltTextMaxLength);
        builder.OwnsOne(s => s.Image, image => image.MapImageFile());
        builder.Navigation(s => s.Image).IsRequired();

        builder.HasIndex(s => s.Slot).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
