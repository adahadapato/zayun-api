using MediatR;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Contact.Commands;

/// <summary>Admin → Inbox: mark a message Replied, Closed or back to New, with an optional private note.</summary>
/// <param name="Id">The message.</param>
/// <param name="Status">The new status.</param>
/// <param name="Note">Optional note; empty removes it.</param>
public sealed record UpdateContactMessageStatusCommand(Guid Id, ContactMessageStatus Status, string? Note)
    : IRequest<Result>;

/// <summary>Handles <see cref="UpdateContactMessageStatusCommand"/>.</summary>
public sealed class UpdateContactMessageStatusCommandHandler(
    IContactMessageRepository messages,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock)
    : IRequestHandler<UpdateContactMessageStatusCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateContactMessageStatusCommand command, CancellationToken ct)
    {
        var adminId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var message = await messages.GetByIdAsync(command.Id, ct);
        if (message is null) return ContactMessageErrors.NotFound;

        message.SetStatus(command.Status, command.Note, adminId, clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

/// <summary>Admin → Inbox: delete a message (soft delete).</summary>
/// <param name="Id">The message.</param>
public sealed record DeleteContactMessageCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteContactMessageCommand"/>.</summary>
public sealed class DeleteContactMessageCommandHandler(
    IContactMessageRepository messages,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
    : IRequestHandler<DeleteContactMessageCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeleteContactMessageCommand command, CancellationToken ct)
    {
        var message = await messages.GetByIdAsync(command.Id, ct);
        if (message is null) return ContactMessageErrors.NotFound;

        message.Delete(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
