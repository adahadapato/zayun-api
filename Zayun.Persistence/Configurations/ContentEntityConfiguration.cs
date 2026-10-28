using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;

namespace Zayun.Persistence.Configurations;

/// <summary>
/// Base for every website-content configuration. Maps what <see cref="ContentEntity"/> adds to every content table
/// (its place in the list and whether it's published), then calls <see cref="ConfigureContent"/> for the rest.
/// </summary>
/// <typeparam name="TEntity">The content type.</typeparam>
public abstract class ContentEntityConfiguration<TEntity> : EntityConfiguration<TEntity>
    where TEntity : ContentEntity
{
    /// <inheritdoc />
    protected sealed override void ConfigureEntity(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(e => e.DisplayOrder).IsRequired();
        builder.Property(e => e.IsPublished).IsRequired();

        builder.HasIndex(e => new { e.IsPublished, e.DisplayOrder });   // the website's list, in order

        ConfigureContent(builder);
    }

    /// <summary>The content's own table, columns, relationships and indexes.</summary>
    /// <param name="builder">EF Core's builder for this entity.</param>
    protected abstract void ConfigureContent(EntityTypeBuilder<TEntity> builder);
}
