using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;

namespace Zayun.Persistence.Configurations;

/// <summary>Table <c>ExperienceAreas</c>. Skills are a JSON list in one column.</summary>
public sealed class ExperienceAreaConfiguration : ContentEntityConfiguration<ExperienceArea>
{
    /// <inheritdoc />
    protected override void ConfigureContent(EntityTypeBuilder<ExperienceArea> builder)
    {
        builder.ToTable("ExperienceAreas");

        builder.Property(e => e.Title).IsRequired().HasMaxLength(ExperienceArea.TitleMaxLength);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(ExperienceArea.DescriptionMaxLength);
        builder.PrimitiveCollection(e => e.Skills).IsRequired();
    }
}
