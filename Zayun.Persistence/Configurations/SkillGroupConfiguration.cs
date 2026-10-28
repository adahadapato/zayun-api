using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;

namespace Zayun.Persistence.Configurations;

/// <summary>Table <c>SkillGroups</c>. Skills are a JSON list in one column.</summary>
public sealed class SkillGroupConfiguration : ContentEntityConfiguration<SkillGroup>
{
    /// <inheritdoc />
    protected override void ConfigureContent(EntityTypeBuilder<SkillGroup> builder)
    {
        builder.ToTable("SkillGroups");

        builder.Property(g => g.Title).IsRequired().HasMaxLength(SkillGroup.TitleMaxLength);
        builder.Property(g => g.Description).IsRequired().HasMaxLength(SkillGroup.DescriptionMaxLength);
        builder.PrimitiveCollection(g => g.Skills).IsRequired();
    }
}
