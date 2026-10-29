using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Common.Tags;
using Zayun.Application.Common.Content;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Projects.Commands;

/// <summary>Admin → Projects: show or hide a project on the website.</summary>
/// <param name="Id">The project.</param>
/// <param name="IsPublished">True to show it, false to hide it.</param>
[InvalidatesCache(CacheTags.Projects)]
public sealed record SetProjectPublishedCommand(Guid Id, bool IsPublished) : IRequest<Result>;

/// <summary>Handles <see cref="SetProjectPublishedCommand"/>.</summary>
public sealed class SetProjectPublishedCommandHandler(IProjectRepository projects, IUnitOfWork unitOfWork)
    : IRequestHandler<SetProjectPublishedCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(SetProjectPublishedCommand command, CancellationToken ct) =>
        ContentActions.SetPublishedAsync(projects, unitOfWork, command.Id, command.IsPublished,
                                         ProjectErrors.NotFound, ct);
}

/// <summary>Admin → Projects: delete a project (soft delete; its picture files are kept).</summary>
/// <param name="Id">The project.</param>
[InvalidatesCache(CacheTags.Projects)]
[RefreshesTagUsage]
public sealed record DeleteProjectCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteProjectCommand"/>.</summary>
public sealed class DeleteProjectCommandHandler(IProjectRepository projects, IUnitOfWork unitOfWork,
                                                TimeProvider clock)
    : IRequestHandler<DeleteProjectCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(DeleteProjectCommand command, CancellationToken ct) =>
        ContentActions.DeleteAsync(projects, unitOfWork, clock, command.Id, ProjectErrors.NotFound, ct);
}

/// <summary>Admin → Projects: drag-to-reorder. Send every project's Id (drafts included) in the new order.</summary>
/// <param name="Ids">Every project's Id, each once.</param>
[InvalidatesCache(CacheTags.Projects)]
public sealed record ReorderProjectsCommand(IReadOnlyList<Guid> Ids) : IRequest<Result>;

/// <summary>Handles <see cref="ReorderProjectsCommand"/>.</summary>
public sealed class ReorderProjectsCommandHandler(IProjectRepository projects, IUnitOfWork unitOfWork)
    : IRequestHandler<ReorderProjectsCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ReorderProjectsCommand command, CancellationToken ct) =>
        ContentActions.ReorderAsync(projects, unitOfWork, command.Ids, ct);
}
