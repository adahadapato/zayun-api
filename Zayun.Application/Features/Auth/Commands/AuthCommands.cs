using MediatR;
using Zayun.Application.Features.Auth.Dtos;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Auth.Commands;

/// <summary>Admin sign-in with email and password.</summary>
/// <param name="Email">The account's email.</param>
/// <param name="Password">The password.</param>
public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthTokensDto>>;

/// <summary>Handles <see cref="LoginCommand"/>.</summary>
public sealed class LoginCommandHandler(IAuthService auth) : IRequestHandler<LoginCommand, Result<AuthTokensDto>>
{
    /// <inheritdoc />
    public async Task<Result<AuthTokensDto>> Handle(LoginCommand c, CancellationToken ct) =>
        (await auth.LoginAsync(c.Email.Trim(), c.Password, ct)).Map(AuthTokensDto.From);
}

/// <summary>Swaps a refresh token for a new pair (the admin area stays signed in).</summary>
/// <param name="RefreshToken">The refresh token.</param>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthTokensDto>>;

/// <summary>Handles <see cref="RefreshTokenCommand"/>.</summary>
public sealed class RefreshTokenCommandHandler(IAuthService auth)
    : IRequestHandler<RefreshTokenCommand, Result<AuthTokensDto>>
{
    /// <inheritdoc />
    public async Task<Result<AuthTokensDto>> Handle(RefreshTokenCommand c, CancellationToken ct) =>
        (await auth.RefreshTokenAsync(c.RefreshToken, ct)).Map(AuthTokensDto.From);
}

/// <summary>Sign out: the refresh token stops working.</summary>
/// <param name="RefreshToken">The refresh token.</param>
public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;

/// <summary>Handles <see cref="LogoutCommand"/>.</summary>
public sealed class LogoutCommandHandler(IAuthService auth) : IRequestHandler<LogoutCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(LogoutCommand c, CancellationToken ct) => auth.RevokeTokenAsync(c.RefreshToken, ct);
}

/// <summary>Admin → Account: change the password (signs out every other session).</summary>
/// <param name="CurrentPassword">The password now.</param>
/// <param name="NewPassword">The new password.</param>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;

/// <summary>Handles <see cref="ChangePasswordCommand"/>.</summary>
public sealed class ChangePasswordCommandHandler(IAuthService auth, ICurrentUser currentUser)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ChangePasswordCommand c, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");
        return auth.ChangePasswordAsync(userId, c.CurrentPassword, c.NewPassword, ct);
    }
}
