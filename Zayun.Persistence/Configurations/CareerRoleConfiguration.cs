using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;

namespace Zayun.Persistence.Configurations;

/// <summary>Table <c>CareerRoles</c> (the "Roles" on the Experience page). Highlights are a JSON list in one column.</summary>
public sealed class CareerRoleConfiguration : ContentEntityConfiguration<CareerRole>
{
    /// <inheritdoc />
    protected override void ConfigureContent(EntityTypeBuilder<CareerRole> builder)
    {
        builder.ToTable("CareerRoles");

        builder.Property(r => r.Kind).IsRequired();
        builder.Property(r => r.Title).IsRequired().HasMaxLength(CareerRole.TitleMaxLength);
        builder.Property(r => r.Organisation).IsRequired().HasMaxLength(CareerRole.OrganisationMaxLength);
        builder.Property(r => r.Location).HasMaxLength(CareerRole.LocationMaxLength);
        builder.Property(r => r.StartYear).IsRequired();
        builder.Property(r => r.Summary).HasMaxLength(CareerRole.SummaryMaxLength);
        builder.PrimitiveCollection(r => r.Highlights).IsRequired();

        builder.Ignore(r => r.IsCurrent);
    }
}
