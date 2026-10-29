using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Features.Profiles.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Profiles.Queries;

/// <summary>The website (footer, Contact, Research, and the data for search engines): published profiles in order.</summary>
[Cacheable(CacheTags.Profiles, Minutes = 60)]
public sealed record GetProfilesQuery : IRequest<Result<IReadOnlyList<ProfileDto>>>;

/// <summary>Handles <see cref="GetProfilesQuery"/>.</summary>
public sealed class GetProfilesQueryHandler(IProfileRepository profiles)
    : IRequestHandler<GetProfilesQuery, Result<IReadOnlyList<ProfileDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProfileDto>>> Handle(GetProfilesQuery query, CancellationToken ct) =>
        Result.Success<IReadOnlyList<ProfileDto>>(
            (await profiles.GetListAsync(publishedOnly: true, ct)).Select(ProfileDto.From).ToList());
}

/// <summary>Admin → Profiles: every profile, drafts included. Never cached.</summary>
public sealed record GetAdminProfilesQuery : IRequest<Result<IReadOnlyList<ProfileDto>>>;

/// <summary>Handles <see cref="GetAdminProfilesQuery"/>.</summary>
public sealed class GetAdminProfilesQueryHandler(IProfileRepository profiles)
    : IRequestHandler<GetAdminProfilesQuery, Result<IReadOnlyList<ProfileDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProfileDto>>> Handle(GetAdminProfilesQuery query, CancellationToken ct) =>
        Result.Success<IReadOnlyList<ProfileDto>>(
            (await profiles.GetListAsync(publishedOnly: false, ct)).Select(ProfileDto.From).ToList());
}
