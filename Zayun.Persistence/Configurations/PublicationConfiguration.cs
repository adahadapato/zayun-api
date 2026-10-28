using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;

namespace Zayun.Persistence.Configurations;

/// <summary>
/// Table <c>Publications</c>. Its links are stored as a JSON list in one column (<c>Links</c>): they're always read
/// and replaced together with the publication.
/// </summary>
public sealed class PublicationConfiguration : ContentEntityConfiguration<Publication>
{
    /// <inheritdoc />
    protected override void ConfigureContent(EntityTypeBuilder<Publication> builder)
    {
        builder.ToTable("Publications");

        builder.Property(p => p.Title).IsRequired().HasMaxLength(Publication.TitleMaxLength);
        builder.Property(p => p.Authors).IsRequired().HasMaxLength(Publication.AuthorsMaxLength);
        builder.Property(p => p.Venue).IsRequired().HasMaxLength(Publication.VenueMaxLength);
        builder.Property(p => p.Year).IsRequired();
        builder.Property(p => p.Type).IsRequired();
        builder.Property(p => p.Award).HasMaxLength(Publication.AwardMaxLength);

        builder.OwnsMany(p => p.Links, links =>
        {
            links.ToJson();
            links.Property(l => l.Label).IsRequired().HasMaxLength(PublicationLink.LabelMaxLength);
            links.Property(l => l.Url).IsRequired().HasMaxLength(PublicationLink.UrlMaxLength);
            links.Property(l => l.IsPrimary).IsRequired();
        });
        builder.Navigation(p => p.Links)
               .HasField("_links")
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
