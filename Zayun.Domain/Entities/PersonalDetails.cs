using Zayun.Domain.Shared.Text;

namespace Zayun.Domain.Entities;

/// <summary>
/// The site owner's personal details, kept in Admin → Account: the name and headline shown on the website, a public
/// email, phone numbers and addresses. One row only.
/// </summary>
/// <remarks>
/// <para><b>Private by default.</b> Each phone number and address has its own "show on the website" switch. A shown
/// address only ever shows its town and country, never the street or postcode.</para>
/// <para>The private details are there for the admin's own use, e.g. CVs made by the job finder later.</para>
/// <para>Bad text is caught by the validators first, so the checks here THROW.</para>
/// </remarks>
public class PersonalDetails : Entity<Guid>
{
    /// <summary>Longest name.</summary>
    public const int NameMaxLength = 120;

    /// <summary>Longest headline, e.g. "Software Engineer and AI Researcher".</summary>
    public const int HeadlineMaxLength = 160;

    /// <summary>Longest email.</summary>
    public const int EmailMaxLength = 256;

    /// <summary>Most phone numbers.</summary>
    public const int MaxPhones = 6;

    /// <summary>Most addresses.</summary>
    public const int MaxAddresses = 5;

    private readonly List<ContactPhone> _phones = [];
    private readonly List<PostalAddress> _addresses = [];

    /// <summary>Required by EF Core.</summary>
    protected PersonalDetails() { }

    private PersonalDetails(Guid id) : base(id) { }

    /// <summary>The name shown on the website, e.g. "Enobong Adahada".</summary>
    public string FullName { get; private set; } = string.Empty;

    /// <summary>A one-line description, e.g. "Software Engineer and AI Researcher"; null for none.</summary>
    public string? Headline { get; private set; }

    /// <summary>The email shown on the website (Contact page); null for none.</summary>
    public string? PublicEmail { get; private set; }

    /// <summary>Phone numbers, in the order shown.</summary>
    public virtual IReadOnlyList<ContactPhone> Phones => _phones.AsReadOnly();

    /// <summary>Addresses (private unless switched on, and then only town and country).</summary>
    public virtual IReadOnlyList<PostalAddress> Addresses => _addresses.AsReadOnly();

    /// <summary>The details, the first time they're saved.</summary>
    /// <param name="details">Everything typed in.</param>
    /// <returns>The row.</returns>
    /// <exception cref="ArgumentException">Missing text.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, or too many phones / addresses.</exception>
    public static PersonalDetails Create(PersonalDetailsInput details)
    {
        var personal = new PersonalDetails(Guid.CreateVersion7());
        personal.Update(details);
        return personal;
    }

    /// <summary>Replaces everything with what was typed in.</summary>
    /// <param name="details">Everything typed in.</param>
    /// <exception cref="ArgumentException">Missing text.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long, or too many phones / addresses.</exception>
    public void Update(PersonalDetailsInput details)
    {
        ArgumentNullException.ThrowIfNull(details);
        var phones = (details.Phones ?? []).Select(p => ContactPhone.Create(p.Label, p.Number, p.ShowOnWebsite)).ToList();
        var addresses = (details.Addresses ?? [])
            .Select(a => PostalAddress.Create(a.Label, a.Line1, a.Line2, a.City, a.Region, a.Postcode, a.Country,
                                              a.ShowOnWebsite))
            .ToList();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(phones.Count, MaxPhones, nameof(details.Phones));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(addresses.Count, MaxAddresses, nameof(details.Addresses));

        FullName = TextRules.Required(details.FullName, NameMaxLength, nameof(details.FullName));
        Headline = TextRules.Optional(details.Headline, HeadlineMaxLength, nameof(details.Headline));
        PublicEmail = TextRules.Optional(details.PublicEmail, EmailMaxLength, nameof(details.PublicEmail));

        _phones.Clear();
        _phones.AddRange(phones);
        _addresses.Clear();
        _addresses.AddRange(addresses);
    }
}

/// <summary>A phone number, e.g. "UK" +44 7881 186690.</summary>
public sealed class ContactPhone
{
    /// <summary>Longest label.</summary>
    public const int LabelMaxLength = 40;

    /// <summary>Longest number.</summary>
    public const int NumberMaxLength = 30;

    private ContactPhone() { }

    /// <summary>What it is, e.g. "UK", "Nigeria", "Mobile".</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>As written, e.g. "+44 7881 186690".</summary>
    public string Number { get; private set; } = string.Empty;

    /// <summary>Shown on the Contact page.</summary>
    public bool ShowOnWebsite { get; private set; }

    /// <summary>A checked phone number.</summary>
    /// <exception cref="ArgumentException">Missing text, or characters a phone number can't have.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long.</exception>
    public static ContactPhone Create(string label, string number, bool showOnWebsite)
    {
        var clean = TextRules.Required(number, NumberMaxLength, nameof(number));
        if (!PhoneNumbers.IsValid(clean)) throw new ArgumentException("Not a phone number.", nameof(number));
        return new ContactPhone
        {
            Label = TextRules.Required(label, LabelMaxLength, nameof(label)),
            Number = clean,
            ShowOnWebsite = showOnWebsite
        };
    }
}

/// <summary>A postal address. When shown on the website, only <see cref="City"/> and <see cref="Country"/> are.</summary>
public sealed class PostalAddress
{
    /// <summary>Longest label.</summary>
    public const int LabelMaxLength = 40;

    /// <summary>Longest address line.</summary>
    public const int LineMaxLength = 200;

    /// <summary>Longest town, region or country.</summary>
    public const int PlaceMaxLength = 100;

    /// <summary>Longest postcode.</summary>
    public const int PostcodeMaxLength = 20;

    private PostalAddress() { }

    /// <summary>What it is, e.g. "Home (UK)".</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>First line.</summary>
    public string Line1 { get; private set; } = string.Empty;

    /// <summary>Second line; null for none.</summary>
    public string? Line2 { get; private set; }

    /// <summary>Town or city.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>County, state or region; null for none.</summary>
    public string? Region { get; private set; }

    /// <summary>Postcode; null for none.</summary>
    public string? Postcode { get; private set; }

    /// <summary>Country.</summary>
    public string Country { get; private set; } = string.Empty;

    /// <summary>Show the town and country on the website (never the rest).</summary>
    public bool ShowOnWebsite { get; private set; }

    /// <summary>A checked address.</summary>
    /// <exception cref="ArgumentException">Missing text.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Text too long.</exception>
    public static PostalAddress Create(string label, string line1, string? line2, string city, string? region,
                                       string? postcode, string country, bool showOnWebsite) =>
        new()
        {
            Label = TextRules.Required(label, LabelMaxLength, nameof(label)),
            Line1 = TextRules.Required(line1, LineMaxLength, nameof(line1)),
            Line2 = TextRules.Optional(line2, LineMaxLength, nameof(line2)),
            City = TextRules.Required(city, PlaceMaxLength, nameof(city)),
            Region = TextRules.Optional(region, PlaceMaxLength, nameof(region)),
            Postcode = TextRules.Optional(postcode, PostcodeMaxLength, nameof(postcode)),
            Country = TextRules.Required(country, PlaceMaxLength, nameof(country)),
            ShowOnWebsite = showOnWebsite
        };
}

/// <summary>Everything typed into Admin → Account → Personal details.</summary>
/// <param name="FullName">The name shown.</param>
/// <param name="Headline">A one-line description, or null.</param>
/// <param name="PublicEmail">The email shown on the website, or null.</param>
/// <param name="Phones">Phone numbers.</param>
/// <param name="Addresses">Addresses.</param>
public sealed record PersonalDetailsInput(
    string FullName,
    string? Headline,
    string? PublicEmail,
    IReadOnlyList<PhoneInput>? Phones,
    IReadOnlyList<AddressInput>? Addresses);

/// <summary>A phone number as typed.</summary>
public sealed record PhoneInput(string Label, string Number, bool ShowOnWebsite);

/// <summary>An address as typed.</summary>
public sealed record AddressInput(string Label, string Line1, string? Line2, string City, string? Region,
                                  string? Postcode, string Country, bool ShowOnWebsite);
