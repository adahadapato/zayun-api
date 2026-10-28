using FluentValidation;
using Zayun.Application.Features.Auth.Commands;

namespace Zayun.Application.Features.Auth.Validators;

/// <summary>Checks <see cref="LoginCommand"/>.</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>The rules.</summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Please enter your email address.")
                             .EmailAddress().WithMessage("Please enter a valid email address.")
                             .MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().WithMessage("Please enter your password.")
                                .MaximumLength(256);
    }
}

/// <summary>Checks <see cref="RefreshTokenCommand"/>.</summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>The rules.</summary>
    public RefreshTokenCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(500);
}

/// <summary>Checks <see cref="LogoutCommand"/>.</summary>
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    /// <summary>The rules.</summary>
    public LogoutCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(500);
}

/// <summary>Checks <see cref="ChangePasswordCommand"/>. Identity checks the password rules themselves.</summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    /// <summary>The rules.</summary>
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Please enter your current password.");
        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("Please choose a new password.")
                                   .MaximumLength(256)
                                   .NotEqual(x => x.CurrentPassword)
                                   .WithMessage("The new password must be different from the current one.");
    }
}
