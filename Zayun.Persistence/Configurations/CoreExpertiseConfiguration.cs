using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;

namespace Zayun.Persistence.Configurations;

/// <summary>Table <c>CoreExpertise</c>: the short list on the Skills page.</summary>
/// <remarks>Names are unique among items that aren't deleted (SQL Server's default collation ignores case).</remarks>
public sealed class CoreExpertiseConfiguration : ContentEntityConfiguration<CoreExpertise>
{
    /// <inheritdoc />
    protected override void ConfigureContent(EntityTypeBuilder<CoreExpertise> builder)
    {
        builder.ToTable("CoreExpertise");

        builder.Property(c => c.Name).IsRequired().HasMaxLength(CoreExpertise.NameMaxLength);
        builder.HasIndex(c => c.Name).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
