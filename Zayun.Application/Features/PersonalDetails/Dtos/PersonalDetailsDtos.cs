using Zayun.Domain.Shared.Text;
using Details = Zayun.Domain.Entities.PersonalDetails;

namespace Zayun.Application.Features.PersonalDetails.Dtos;

/// <summary>Everything in Admin → Account → Personal details (private: admin only).</summary>
/// <param name="FullName">The name shown.</param>
/// <param name="Headline">A one-line description.</param>
/// <param name="PublicEmail">The email shown on the website.</param>
/// <param name="Phones">Phone numbers.</param>
/// <param name="Addresses">Addresses.</param>
public sealed record PersonalDetailsDto(
    string FullName,
    string? Headline,
    string? PublicEmail,
    IReadOnlyList<PhoneDto> Phones,
    IReadOnlyList<AddressDto> Addresses)
{
    /// <summary>Maps the details; null gives the empty form.</summary>
    public static PersonalDetailsDto From(Details? d) =>
        d is null
            ? new PersonalDetailsDto(string.Empty, null, null, [], [])
            : new PersonalDetailsDto(
                d.FullName, d.Headline, d.PublicEmail,
                d.Phones.Select(p => new PhoneDto(p.Label, p.Number, p.ShowOnWebsite)).ToList(),
                d.Addresses.Select(a => new AddressDto(a.Label, a.Line1, a.Line2, a.City, a.Region, a.Postcode,
                                                       a.Country, a.ShowOnWebsite)).ToList());
}

/// <summary>A phone number.</summary>
public sealed record PhoneDto(string Label, string Number, bool ShowOnWebsite);

/// <summary>An address.</summary>
public sealed record AddressDto(string Label, string Line1, string? Line2, string City, string? Region,
                                string? Postcode, string Country, bool ShowOnWebsite);

/// <summary>What the WEBSITE may show: only the items switched on, and addresses only as town and country.</summary>
/// <param name="FullName">The name shown.</param>
/// <param name="Headline">A one-line description.</param>
/// <param name="Email">The public email, if any.</param>
/// <param name="Phones">Phone numbers switched on.</param>
/// <param name="Locations">"London, United Kingdom" for each address switched on.</param>
public sealed record PublicPersonalDetailsDto(
    string FullName,
    string? Headline,
    string? Email,
    IReadOnlyList<PublicPhoneDto> Phones,
    IReadOnlyList<string> Locations)
{
    /// <summary>Maps the shown parts only.</summary>
    public static PublicPersonalDetailsDto From(Details? d) =>
        d is null
            ? new PublicPersonalDetailsDto(string.Empty, null, null, [], [])
            : new PublicPersonalDetailsDto(
                d.FullName, d.Headline, d.PublicEmail,
                d.Phones.Where(p => p.ShowOnWebsite)
                        .Select(p => new PublicPhoneDto(p.Label, p.Number, PhoneNumbers.ForLink(p.Number))).ToList(),
                d.Addresses.Where(a => a.ShowOnWebsite).Select(a => $"{a.City}, {a.Country}").Distinct().ToList());
}

/// <summary>A phone number shown on the website.</summary>
/// <param name="Label">e.g. "UK".</param>
/// <param name="Number">As written.</param>
/// <param name="Link">For tel: links, e.g. "+447881186690".</param>
public sealed record PublicPhoneDto(string Label, string Number, string Link);
