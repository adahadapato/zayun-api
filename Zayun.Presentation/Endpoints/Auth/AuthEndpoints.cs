using MediatR;
using Zayun.Application.Features.Auth.Commands;
using Zayun.Application.Features.Auth.Dtos;
using Zayun.Application.Features.Auth.Queries;
using Zayun.Domain.Constants;
using Zayun.Presentation.Extensions;
using Zayun.Presentation.Settings.RateLimiting;

namespace Zayun.Presentation.Endpoints.Auth;

/// <summary><c>/api/auth</c>: admin sign-in, tokens, the signed-in account and password change.</summary>
public static class AuthEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Auth");

        auth.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingSetup.SignInPolicy)
            .WithName("Login")
            .WithSummary("Sign in to the admin area with email and password; returns an access + refresh token")
            .WithDescription("Only Admin accounts can sign in. Five wrong passwords lock the account for a while.")
            .Produces<AuthTokensDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/refresh", async (RefreshTokenCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingSetup.SignInPolicy)
            .WithName("RefreshToken")
            .WithSummary("Swap the refresh token for a new pair (the old refresh token stops working)")
            .Produces<AuthTokensDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/logout", async (LogoutCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .AllowAnonymous()
            .WithName("Logout")
            .WithSummary("Sign out: the refresh token can't be used again")
            .Produces(StatusCodes.Status204NoContent);

        auth.MapGet("/me", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMyAccountQuery(), ct)).ToHttp())
            .RequireAuthorization(Policies.RequireAdmin)
            .WithName("GetMyAccount")
            .WithSummary("The signed-in admin (the admin area calls this to check its session)")
            .Produces<AdminAccountDto>();

        auth.MapPut("/password", async (ChangePasswordCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .RequireAuthorization(Policies.RequireAdmin)
            .WithName("ChangePassword")
            .WithSummary("Change my password; signs out every session, so sign in again afterwards")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        return api;
    }
}
