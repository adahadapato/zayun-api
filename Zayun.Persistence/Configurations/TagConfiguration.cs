using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;
using Zayun.Domain.Shared.Text;

namespace Zayun.Persistence.Configurations;

/// <summary>
/// Table <c>Tags</c>: the shared tag list. <c>NormalizedName</c> (the UPPERCASE key) is unique, so the same tag can't
/// be stored twice; aliases are a JSON list in one column.
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

        builder.HasIndex(t => t.NormalizedName).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
