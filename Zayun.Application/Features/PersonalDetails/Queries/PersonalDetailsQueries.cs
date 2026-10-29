using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.PersonalDetails.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.PersonalDetails.Queries;

/// <summary>The website (Contact page, search engines): only what's switched on for the website.</summary>
[Cacheable(CacheTags.PersonalDetails, Minutes = 60)]
public sealed record GetPublicPersonalDetailsQuery : IRequest<Result<PublicPersonalDetailsDto>>;

/// <summary>Handles <see cref="GetPublicPersonalDetailsQuery"/>.</summary>
public sealed class GetPublicPersonalDetailsQueryHandler(IPersonalDetailsRepository details)
    : IRequestHandler<GetPublicPersonalDetailsQuery, Result<PublicPersonalDetailsDto>>
{
    /// <inheritdoc />
    public async Task<Result<PublicPersonalDetailsDto>> Handle(GetPublicPersonalDetailsQuery query, CancellationToken ct) =>
        PublicPersonalDetailsDto.From(await details.GetAsync(ct));
}

/// <summary>Admin → Account: everything, private parts included. Never cached.</summary>
public sealed record GetPersonalDetailsQuery : IRequest<Result<PersonalDetailsDto>>;

/// <summary>Handles <see cref="GetPersonalDetailsQuery"/>.</summary>
public sealed class GetPersonalDetailsQueryHandler(IPersonalDetailsRepository details)
    : IRequestHandler<GetPersonalDetailsQuery, Result<PersonalDetailsDto>>
{
    /// <inheritdoc />
    public async Task<Result<PersonalDetailsDto>> Handle(GetPersonalDetailsQuery query, CancellationToken ct) =>
        PersonalDetailsDto.From(await details.GetAsync(ct));
}
