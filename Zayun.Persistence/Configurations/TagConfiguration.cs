using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;
using Zayun.Domain.Shared.Text;

namespace Zayun.Persistence.Configurations;

/// <summary>
/// Table <c>Tags</c>: the shared tag list. <c>NormalizedName</c> (the UPPERCASE key) is unique, so the same tag can't
/// be stored twice. <c>SearchKeys</c> ("|KEY|ALIAS|") lets the database find a tag by any of its keys; the usage
/// counts are indexed for the "most used" order.
/// </summary>
public sealed class TagConfiguration : EntityConfiguration<Tag>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.Property(t => t.Name).IsRequired().HasMaxLength(TagNames.MaxLength);
        builder.Property(t => t.NormalizedName).IsRequired().HasMaxLength(TagNames.KeyMaxLength);
        builder.PrimitiveCollection(t => t.Aliases).IsRequired();
        builder.Property(t => t.SearchKeys).IsRequired().HasMaxLength(Tag.SearchKeysMaxLength);
        builder.Property(t => t.ProjectCount).IsRequired().HasDefaultValue(0);
        builder.Property(t => t.ExperienceAreaCount).IsRequired().HasDefaultValue(0);
        builder.Property(t => t.SkillGroupCount).IsRequired().HasDefaultValue(0);
        builder.Ignore(t => t.UsageCount);

        builder.HasIndex(t => t.NormalizedName).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(t => t.Name);
    }
}
