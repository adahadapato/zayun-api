using MediatR;
using Zayun.Application.Features.Auth.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Auth.Queries;

/// <summary>Who the signed-in admin is (the admin area calls this on start-up to check the session).</summary>
public sealed record GetMyAccountQuery : IRequest<Result<AdminAccountDto>>;

/// <summary>Handles <see cref="GetMyAccountQuery"/>.</summary>
public sealed class GetMyAccountQueryHandler(IAuthService auth, ICurrentUser currentUser)
    : IRequestHandler<GetMyAccountQuery, Result<AdminAccountDto>>
{
    /// <inheritdoc />
    public async Task<Result<AdminAccountDto>> Handle(GetMyAccountQuery query, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");
        return (await auth.GetAccountAsync(userId, ct)).Map(AdminAccountDto.From);
    }
}
