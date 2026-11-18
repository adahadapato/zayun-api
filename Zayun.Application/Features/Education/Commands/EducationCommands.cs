using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Common.Content;
using Zayun.Application.Features.Education.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Education.Commands;

/// <summary>Admin → New qualification. Its place on the website comes from its year.</summary>
/// <param name="Degree">The degree or award.</param>
/// <param name="Subject">The subject.</param>
/// <param name="Institution">Where.</param>
/// <param name="Location">The place (optional).</param>
/// <param name="Year">The year completed; null while in progress.</param>
/// <param name="Note">A grade or note (optional).</param>
/// <param name="IsPublished">Show it straight away, or keep it as a draft.</param>
[InvalidatesCache(CacheTags.Education)]
public sealed record CreateQualificationCommand(string Degree, string Subject, string Institution, string? Location,
                                                int? Year, string? Note, bool IsPublished)
    : IRequest<Result<QualificationDto>>, IQualificationFields;

/// <summary>Handles <see cref="CreateQualificationCommand"/>.</summary>
public sealed class CreateQualificationCommandHandler(IQualificationRepository qualifications, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateQualificationCommand, Result<QualificationDto>>
{
    /// <inheritdoc />
    public async Task<Result<QualificationDto>> Handle(CreateQualificationCommand c, CancellationToken ct)
    {
        var qualification = Qualification.Create(c.ToDetails(), await qualifications.GetNextDisplayOrderAsync(ct),
                                                 c.IsPublished);
        qualifications.Add(qualification);
        await unitOfWork.SaveChangesAsync(ct);
        return QualificationDto.From(qualification);
    }
}

/// <summary>Admin → Edit qualification: every detail.</summary>
/// <param name="Id">The qualification.</param>
/// <param name="Degree">The degree or award.</param>
/// <param name="Subject">The subject.</param>
/// <param name="Institution">Where.</param>
/// <param name="Location">The place (optional).</param>
/// <param name="Year">The year completed; null while in progress.</param>
/// <param name="Note">A grade or note (optional).</param>
[InvalidatesCache(CacheTags.Education)]
public sealed record UpdateQualificationCommand(Guid Id, string Degree, string Subject, string Institution,
                                                string? Location, int? Year, string? Note)
    : IRequest<Result<QualificationDto>>, IQualificationFields;

/// <summary>Handles <see cref="UpdateQualificationCommand"/>.</summary>
public sealed class UpdateQualificationCommandHandler(IQualificationRepository qualifications, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateQualificationCommand, Result<QualificationDto>>
{
    /// <inheritdoc />
    public async Task<Result<QualificationDto>> Handle(UpdateQualificationCommand c, CancellationToken ct)
    {
        var qualification = await qualifications.GetByIdAsync(c.Id, ct);
        if (qualification is null) return QualificationErrors.NotFound;

        qualification.Update(c.ToDetails());
        await unitOfWork.SaveChangesAsync(ct);
        return QualificationDto.From(qualification);
    }
}

/// <summary>Admin → Education: show or hide a qualification.</summary>
/// <param name="Id">The qualification.</param>
/// <param name="IsPublished">True to show it.</param>
[InvalidatesCache(CacheTags.Education)]
public sealed record SetQualificationPublishedCommand(Guid Id, bool IsPublished) : IRequest<Result>;

/// <summary>Handles <see cref="SetQualificationPublishedCommand"/>.</summary>
public sealed class SetQualificationPublishedCommandHandler(IQualificationRepository qualifications,
                                                            IUnitOfWork unitOfWork)
    : IRequestHandler<SetQualificationPublishedCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(SetQualificationPublishedCommand c, CancellationToken ct) =>
        ContentActions.SetPublishedAsync(qualifications, unitOfWork, c.Id, c.IsPublished,
                                         QualificationErrors.NotFound, ct);
}

/// <summary>Admin → Education: delete a qualification (soft delete).</summary>
/// <param name="Id">The qualification.</param>
[InvalidatesCache(CacheTags.Education)]
public sealed record DeleteQualificationCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteQualificationCommand"/>.</summary>
public sealed class DeleteQualificationCommandHandler(IQualificationRepository qualifications, IUnitOfWork unitOfWork,
                                                      TimeProvider clock)
    : IRequestHandler<DeleteQualificationCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(DeleteQualificationCommand c, CancellationToken ct) =>
        ContentActions.DeleteAsync(qualifications, unitOfWork, clock, c.Id, QualificationErrors.NotFound, ct);
}

/// <summary>The fields a new or edited qualification has, so both commands share one set of checks.</summary>
public interface IQualificationFields
{
    /// <summary>The degree or award.</summary>
    string Degree { get; }

    /// <summary>The subject.</summary>
    string Subject { get; }

    /// <summary>Where.</summary>
    string Institution { get; }

    /// <summary>The place (optional).</summary>
    string? Location { get; }

    /// <summary>The year completed; null while in progress.</summary>
    int? Year { get; }

    /// <summary>A grade or note (optional).</summary>
    string? Note { get; }
}

/// <summary>Turns the fields into the entity's details.</summary>
internal static class QualificationFieldsExtensions
{
    /// <summary>The entity's details.</summary>
    /// <param name="f">The fields.</param>
    public static QualificationDetails ToDetails(this IQualificationFields f) =>
        new(f.Degree, f.Subject, f.Institution, f.Location, f.Year, f.Note);
}
