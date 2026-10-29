using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Common.Content;
using Zayun.Application.Features.Profiles.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Profiles.Commands;

/// <summary>Admin → New profile. It goes to the end of the list.</summary>
/// <param name="Platform">Which site (picks the icon).</param>
/// <param name="Name">The name shown.</param>
/// <param name="Url">The profile's address.</param>
/// <param name="Label">An optional short line, e.g. the ORCID iD.</param>
/// <param name="ShowInFooter">Show in the footer.</param>
/// <param name="ShowOnContact">Show on the Contact page.</param>
/// <param name="ShowOnResearch">Show on the Research page.</param>
/// <param name="IsPublished">Show it straight away, or keep it as a draft.</param>
[InvalidatesCache(CacheTags.Profiles)]
public sealed record CreateProfileCommand(
    ProfilePlatform Platform,
    string Name,
    string Url,
    string? Label,
    bool ShowInFooter,
    bool ShowOnContact,
    bool ShowOnResearch,
    bool IsPublished) : IRequest<Result<ProfileDto>>;

/// <summary>Handles <see cref="CreateProfileCommand"/>.</summary>
public sealed class CreateProfileCommandHandler(IProfileRepository profiles, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProfileCommand, Result<ProfileDto>>
{
    /// <inheritdoc />
    public async Task<Result<ProfileDto>> Handle(CreateProfileCommand c, CancellationToken ct)
    {
        var profile = Profile.Create(
            new ProfileDetails(c.Platform, c.Name, c.Url, c.Label, c.ShowInFooter, c.ShowOnContact, c.ShowOnResearch),
            await profiles.GetNextDisplayOrderAsync(ct),
            c.IsPublished);

        profiles.Add(profile);
        await unitOfWork.SaveChangesAsync(ct);
        return ProfileDto.From(profile);
    }
}

/// <summary>Admin → Edit profile.</summary>
/// <param name="Id">The profile.</param>
/// <param name="Platform">Which site.</param>
/// <param name="Name">The name shown.</param>
/// <param name="Url">The profile's address.</param>
/// <param name="Label">An optional short line.</param>
/// <param name="ShowInFooter">Show in the footer.</param>
/// <param name="ShowOnContact">Show on the Contact page.</param>
/// <param name="ShowOnResearch">Show on the Research page.</param>
[InvalidatesCache(CacheTags.Profiles)]
public sealed record UpdateProfileCommand(
    Guid Id,
    ProfilePlatform Platform,
    string Name,
    string Url,
    string? Label,
    bool ShowInFooter,
    bool ShowOnContact,
    bool ShowOnResearch) : IRequest<Result<ProfileDto>>;

/// <summary>Handles <see cref="UpdateProfileCommand"/>.</summary>
public sealed class UpdateProfileCommandHandler(IProfileRepository profiles, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProfileCommand, Result<ProfileDto>>
{
    /// <inheritdoc />
    public async Task<Result<ProfileDto>> Handle(UpdateProfileCommand c, CancellationToken ct)
    {
        var profile = await profiles.GetByIdAsync(c.Id, ct);
        if (profile is null) return ProfileErrors.NotFound;

        profile.Update(new ProfileDetails(c.Platform, c.Name, c.Url, c.Label, c.ShowInFooter, c.ShowOnContact,
                                          c.ShowOnResearch));
        await unitOfWork.SaveChangesAsync(ct);
        return ProfileDto.From(profile);
    }
}

/// <summary>Admin → Profiles: show or hide a profile.</summary>
/// <param name="Id">The profile.</param>
/// <param name="IsPublished">True to show it.</param>
[InvalidatesCache(CacheTags.Profiles)]
public sealed record SetProfilePublishedCommand(Guid Id, bool IsPublished) : IRequest<Result>;

/// <summary>Handles <see cref="SetProfilePublishedCommand"/>.</summary>
public sealed class SetProfilePublishedCommandHandler(IProfileRepository profiles, IUnitOfWork unitOfWork)
    : IRequestHandler<SetProfilePublishedCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(SetProfilePublishedCommand c, CancellationToken ct) =>
        ContentActions.SetPublishedAsync(profiles, unitOfWork, c.Id, c.IsPublished, ProfileErrors.NotFound, ct);
}

/// <summary>Admin → Profiles: delete a profile.</summary>
/// <param name="Id">The profile.</param>
[InvalidatesCache(CacheTags.Profiles)]
public sealed record DeleteProfileCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteProfileCommand"/>.</summary>
public sealed class DeleteProfileCommandHandler(IProfileRepository profiles, IUnitOfWork unitOfWork, TimeProvider clock)
    : IRequestHandler<DeleteProfileCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(DeleteProfileCommand c, CancellationToken ct) =>
        ContentActions.DeleteAsync(profiles, unitOfWork, clock, c.Id, ProfileErrors.NotFound, ct);
}

/// <summary>Admin → Profiles: reorder. Send every profile's Id in the new order.</summary>
/// <param name="Ids">Every profile's Id, each once.</param>
[InvalidatesCache(CacheTags.Profiles)]
public sealed record ReorderProfilesCommand(IReadOnlyList<Guid> Ids) : IRequest<Result>;

/// <summary>Handles <see cref="ReorderProfilesCommand"/>.</summary>
public sealed class ReorderProfilesCommandHandler(IProfileRepository profiles, IUnitOfWork unitOfWork)
    : IRequestHandler<ReorderProfilesCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ReorderProfilesCommand c, CancellationToken ct) =>
        ContentActions.ReorderAsync(profiles, unitOfWork, c.Ids, ct);
}
