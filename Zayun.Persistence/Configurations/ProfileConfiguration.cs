using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;

namespace Zayun.Persistence.Configurations;

/// <summary>Table <c>Profiles</c>: online profiles (ORCID, ResearchGate, LinkedIn…) and where each is shown.</summary>
public sealed class ProfileConfiguration : ContentEntityConfiguration<Profile>
{
    /// <inheritdoc />
    protected override void ConfigureContent(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("Profiles");

        builder.Property(p => p.Platform).IsRequired();
        builder.Property(p => p.Name).IsRequired().HasMaxLength(Profile.NameMaxLength);
        builder.Property(p => p.Url).IsRequired().HasMaxLength(Profile.UrlMaxLength);
        builder.Property(p => p.Label).HasMaxLength(Profile.LabelMaxLength);
        builder.Property(p => p.ShowInFooter).IsRequired();
        builder.Property(p => p.ShowOnContact).IsRequired();
        builder.Property(p => p.ShowOnResearch).IsRequired();
    }
}
