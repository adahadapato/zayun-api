using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;

namespace Zayun.Persistence.Configurations;

/// <summary>Table <c>Qualifications</c> (the "Education" on the Experience page and the CVs).</summary>
public sealed class QualificationConfiguration : ContentEntityConfiguration<Qualification>
{
    /// <inheritdoc />
    protected override void ConfigureContent(EntityTypeBuilder<Qualification> builder)
    {
        builder.ToTable("Qualifications");

        builder.Property(q => q.Degree).IsRequired().HasMaxLength(Qualification.DegreeMaxLength);
        builder.Property(q => q.Subject).IsRequired().HasMaxLength(Qualification.SubjectMaxLength);
        builder.Property(q => q.Institution).IsRequired().HasMaxLength(Qualification.InstitutionMaxLength);
        builder.Property(q => q.Location).HasMaxLength(Qualification.LocationMaxLength);
        builder.Property(q => q.Note).HasMaxLength(Qualification.NoteMaxLength);

        builder.Ignore(q => q.IsInProgress);
    }
}
