using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.PersonalDetails.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;
using Details = Zayun.Domain.Entities.PersonalDetails;

namespace Zayun.Application.Features.PersonalDetails.Commands;

/// <summary>Admin → Account → Personal details → Save: replaces everything with what was typed in.</summary>
/// <param name="FullName">The name shown.</param>
/// <param name="Headline">A one-line description.</param>
/// <param name="PublicEmail">The email shown on the website.</param>
/// <param name="Phones">Phone numbers.</param>
/// <param name="Addresses">Addresses.</param>
[InvalidatesCache(CacheTags.PersonalDetails)]
public sealed record SavePersonalDetailsCommand(
    string FullName,
    string? Headline,
    string? PublicEmail,
    IReadOnlyList<PhoneDto>? Phones,
    IReadOnlyList<AddressDto>? Addresses) : IRequest<Result<PersonalDetailsDto>>;

/// <summary>Handles <see cref="SavePersonalDetailsCommand"/>.</summary>
public sealed class SavePersonalDetailsCommandHandler(IPersonalDetailsRepository details, IUnitOfWork unitOfWork)
    : IRequestHandler<SavePersonalDetailsCommand, Result<PersonalDetailsDto>>
{
    /// <inheritdoc />
    public async Task<Result<PersonalDetailsDto>> Handle(SavePersonalDetailsCommand c, CancellationToken ct)
    {
        var input = new PersonalDetailsInput(
            c.FullName, c.Headline, c.PublicEmail,
            (c.Phones ?? []).Select(p => new PhoneInput(p.Label, p.Number, p.ShowOnWebsite)).ToList(),
            (c.Addresses ?? []).Select(a => new AddressInput(a.Label, a.Line1, a.Line2, a.City, a.Region, a.Postcode,
                                                             a.Country, a.ShowOnWebsite)).ToList());

        var existing = await details.GetAsync(ct);
        if (existing is null)
        {
            existing = Details.Create(input);
            details.Add(existing);
        }
        else
        {
            existing.Update(input);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return PersonalDetailsDto.From(existing);
    }
}
