using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;
using Zayun.Domain.Shared.Text;

namespace Zayun.Persistence.Configurations;

/// <summary>
/// Table <c>Projects</c>, with its pictures in <c>ProjectImages</c> (configured here, through the private
/// <c>_images</c> field). Tags are a JSON list in one column.
/// </summary>
/// <remarks>
/// The web address is unique among projects that aren't deleted (a filtered index), so a deleted project's address
/// can be used again.
/// </remarks>
public sealed class ProjectConfiguration : ContentEntityConfiguration<Project>
{
    /// <inheritdoc />
    protected override void ConfigureContent(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.Property(p => p.Title).IsRequired().HasMaxLength(Project.TitleMaxLength);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(Slugs.MaxLength);
        builder.Property(p => p.Subtitle).IsRequired().HasMaxLength(Project.SubtitleMaxLength);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(Project.DescriptionMaxLength);
        builder.Property(p => p.Category).IsRequired();
        builder.Property(p => p.Label).IsRequired().HasMaxLength(Project.LabelMaxLength);
        builder.Property(p => p.VisualLabel).HasMaxLength(Project.VisualLabelMaxLength);
        builder.Property(p => p.GithubUrl).HasMaxLength(Project.UrlMaxLength);
        builder.Property(p => p.LiveUrl).HasMaxLength(Project.UrlMaxLength);
        builder.Property(p => p.IsPrivateRepo).IsRequired();
        builder.PrimitiveCollection(p => p.Tags).IsRequired();

        // ---------- Pictures ----------
        builder.HasMany(p => p.Images)
               .WithOne()
               .HasForeignKey(i => i.ProjectId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Images)
               .HasField("_images")
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        // ---------- Indexes ----------
        builder.HasIndex(p => p.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(p => new { p.Category, p.DisplayOrder });
    }
}
