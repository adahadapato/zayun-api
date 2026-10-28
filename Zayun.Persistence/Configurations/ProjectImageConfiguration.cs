using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;
using Zayun.Persistence.Mappings;

namespace Zayun.Persistence.Configurations;

/// <summary>
/// Table <c>ProjectImages</c>: one processed picture of a project, with the file's details stored as columns
/// (<c>File_Path</c>, <c>File_Dimensions_Width</c>…). Child of <see cref="Project"/>; the relationship is configured
/// in <see cref="ProjectConfiguration"/>.
/// </summary>
/// <remarks>Removing a picture through <see cref="Project.RemoveImage"/> deletes this row; the handler deletes the
/// files.</remarks>
public sealed class ProjectImageConfiguration : EntityConfiguration<ProjectImage>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<ProjectImage> builder)
    {
        builder.ToTable("ProjectImages");

        builder.Property(i => i.ProjectId).IsRequired();
        builder.Property(i => i.AltText).IsRequired().HasMaxLength(ProjectImage.AltTextMaxLength);
        builder.Property(i => i.Label).HasMaxLength(ProjectImage.LabelMaxLength);
        builder.Property(i => i.Kind).IsRequired();
        builder.Property(i => i.DisplayOrder).IsRequired();

        builder.OwnsOne(i => i.File, file => file.MapImageFile());
        builder.Navigation(i => i.File).IsRequired();

        builder.HasIndex(i => new { i.ProjectId, i.DisplayOrder });   // pictures in order
    }
}
