using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;

namespace Zayun.Persistence.Configurations;

/// <summary>Table <c>PersonalDetails</c> (one row). Phone numbers and addresses are JSON lists in their own columns.</summary>
public sealed class PersonalDetailsConfiguration : EntityConfiguration<PersonalDetails>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<PersonalDetails> builder)
    {
        builder.ToTable("PersonalDetails");

        builder.Property(p => p.FullName).IsRequired().HasMaxLength(PersonalDetails.NameMaxLength);
        builder.Property(p => p.Headline).HasMaxLength(PersonalDetails.HeadlineMaxLength);
        builder.Property(p => p.PublicEmail).HasMaxLength(PersonalDetails.EmailMaxLength);

        builder.OwnsMany(p => p.Phones, phones =>
        {
            phones.ToJson();
            phones.Property(x => x.Label).IsRequired().HasMaxLength(ContactPhone.LabelMaxLength);
            phones.Property(x => x.Number).IsRequired().HasMaxLength(ContactPhone.NumberMaxLength);
            phones.Property(x => x.ShowOnWebsite).IsRequired();
        });
        builder.Navigation(p => p.Phones).HasField("_phones").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(p => p.Addresses, addresses =>
        {
            addresses.ToJson();
            addresses.Property(x => x.Label).IsRequired().HasMaxLength(PostalAddress.LabelMaxLength);
            addresses.Property(x => x.Line1).IsRequired().HasMaxLength(PostalAddress.LineMaxLength);
            addresses.Property(x => x.Line2).HasMaxLength(PostalAddress.LineMaxLength);
            addresses.Property(x => x.City).IsRequired().HasMaxLength(PostalAddress.PlaceMaxLength);
            addresses.Property(x => x.Region).HasMaxLength(PostalAddress.PlaceMaxLength);
            addresses.Property(x => x.Postcode).HasMaxLength(PostalAddress.PostcodeMaxLength);
            addresses.Property(x => x.Country).IsRequired().HasMaxLength(PostalAddress.PlaceMaxLength);
            addresses.Property(x => x.ShowOnWebsite).IsRequired();
        });
        builder.Navigation(p => p.Addresses).HasField("_addresses").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
