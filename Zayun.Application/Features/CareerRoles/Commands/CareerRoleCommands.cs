using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Common.Content;
using Zayun.Application.Features.CareerRoles.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.CareerRoles.Commands;

/// <summary>Admin → New role. Its place on the website comes from its dates.</summary>
/// <param name="Kind">Professional, research, teaching or other.</param>
/// <param name="Title">The job title.</param>
/// <param name="Organisation">Where.</param>
/// <param name="Location">The place (optional).</param>
/// <param name="StartYear">The year it started.</param>
/// <param name="StartMonth">The month it started (optional).</param>
/// <param name="EndYear">The year it ended; null while current.</param>
/// <param name="EndMonth">The month it ended (optional; needs an end year).</param>
/// <param name="Summary">One line about the role (optional).</param>
/// <param name="Highlights">Achievement bullets (optional).</param>
/// <param name="IsPublished">Show it straight away, or keep it as a draft.</param>
[InvalidatesCache(CacheTags.CareerRoles)]
public sealed record CreateCareerRoleCommand(
    CareerRoleKind Kind,
    string Title,
    string Organisation,
    string? Location,
    int StartYear,
    int? StartMonth,
    int? EndYear,
    int? EndMonth,
    string? Summary,
    IReadOnlyList<string>? Highlights,
    bool IsPublished) : IRequest<Result<CareerRoleDto>>, ICareerRoleFields;

/// <summary>Handles <see cref="CreateCareerRoleCommand"/>.</summary>
public sealed class CreateCareerRoleCommandHandler(ICareerRoleRepository roles, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCareerRoleCommand, Result<CareerRoleDto>>
{
    /// <inheritdoc />
    public async Task<Result<CareerRoleDto>> Handle(CreateCareerRoleCommand c, CancellationToken ct)
    {
        var role = CareerRole.Create(c.ToDetails(), await roles.GetNextDisplayOrderAsync(ct), c.IsPublished);
        roles.Add(role);
        await unitOfWork.SaveChangesAsync(ct);
        return CareerRoleDto.From(role);
    }
}

/// <summary>Admin → Edit role: every detail.</summary>
/// <param name="Id">The role.</param>
/// <param name="Kind">Professional, research, teaching or other.</param>
/// <param name="Title">The job title.</param>
/// <param name="Organisation">Where.</param>
/// <param name="Location">The place (optional).</param>
/// <param name="StartYear">The year it started.</param>
/// <param name="StartMonth">The month it started (optional).</param>
/// <param name="EndYear">The year it ended; null while current.</param>
/// <param name="EndMonth">The month it ended (optional; needs an end year).</param>
/// <param name="Summary">One line about the role (optional).</param>
/// <param name="Highlights">Achievement bullets (replaces the old list).</param>
[InvalidatesCache(CacheTags.CareerRoles)]
public sealed record UpdateCareerRoleCommand(
    Guid Id,
    CareerRoleKind Kind,
    string Title,
    string Organisation,
    string? Location,
    int StartYear,
    int? StartMonth,
    int? EndYear,
    int? EndMonth,
    string? Summary,
    IReadOnlyList<string>? Highlights) : IRequest<Result<CareerRoleDto>>, ICareerRoleFields;

/// <summary>Handles <see cref="UpdateCareerRoleCommand"/>.</summary>
public sealed class UpdateCareerRoleCommandHandler(ICareerRoleRepository roles, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCareerRoleCommand, Result<CareerRoleDto>>
{
    /// <inheritdoc />
    public async Task<Result<CareerRoleDto>> Handle(UpdateCareerRoleCommand c, CancellationToken ct)
    {
        var role = await roles.GetByIdAsync(c.Id, ct);
        if (role is null) return CareerRoleErrors.NotFound;

        role.Update(c.ToDetails());
        await unitOfWork.SaveChangesAsync(ct);
        return CareerRoleDto.From(role);
    }
}

/// <summary>Admin → Roles: show or hide a role.</summary>
/// <param name="Id">The role.</param>
/// <param name="IsPublished">True to show it.</param>
[InvalidatesCache(CacheTags.CareerRoles)]
public sealed record SetCareerRolePublishedCommand(Guid Id, bool IsPublished) : IRequest<Result>;

/// <summary>Handles <see cref="SetCareerRolePublishedCommand"/>.</summary>
public sealed class SetCareerRolePublishedCommandHandler(ICareerRoleRepository roles, IUnitOfWork unitOfWork)
    : IRequestHandler<SetCareerRolePublishedCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(SetCareerRolePublishedCommand c, CancellationToken ct) =>
        ContentActions.SetPublishedAsync(roles, unitOfWork, c.Id, c.IsPublished, CareerRoleErrors.NotFound, ct);
}

/// <summary>Admin → Roles: delete a role (soft delete).</summary>
/// <param name="Id">The role.</param>
[InvalidatesCache(CacheTags.CareerRoles)]
public sealed record DeleteCareerRoleCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteCareerRoleCommand"/>.</summary>
public sealed class DeleteCareerRoleCommandHandler(ICareerRoleRepository roles, IUnitOfWork unitOfWork,
                                                   TimeProvider clock)
    : IRequestHandler<DeleteCareerRoleCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(DeleteCareerRoleCommand c, CancellationToken ct) =>
        ContentActions.DeleteAsync(roles, unitOfWork, clock, c.Id, CareerRoleErrors.NotFound, ct);
}

/// <summary>The fields a new or edited role has, so both commands share one set of checks.</summary>
public interface ICareerRoleFields
{
    /// <summary>Professional, research, teaching or other.</summary>
    CareerRoleKind Kind { get; }

    /// <summary>The job title.</summary>
    string Title { get; }

    /// <summary>Where.</summary>
    string Organisation { get; }

    /// <summary>The place (optional).</summary>
    string? Location { get; }

    /// <summary>The year it started.</summary>
    int StartYear { get; }

    /// <summary>The month it started (optional).</summary>
    int? StartMonth { get; }

    /// <summary>The year it ended; null while current.</summary>
    int? EndYear { get; }

    /// <summary>The month it ended (optional).</summary>
    int? EndMonth { get; }

    /// <summary>One line about the role (optional).</summary>
    string? Summary { get; }

    /// <summary>Achievement bullets (optional).</summary>
    IReadOnlyList<string>? Highlights { get; }
}

/// <summary>Turns the fields into the entity's details.</summary>
internal static class CareerRoleFieldsExtensions
{
    /// <summary>The entity's details.</summary>
    /// <param name="f">The fields.</param>
    public static CareerRoleDetails ToDetails(this ICareerRoleFields f) =>
        new(f.Kind, f.Title, f.Organisation, f.Location, f.StartYear, f.StartMonth, f.EndYear, f.EndMonth, f.Summary,
            f.Highlights);
}
