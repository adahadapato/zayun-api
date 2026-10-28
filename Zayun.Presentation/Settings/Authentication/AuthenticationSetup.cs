using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Zayun.Domain.Constants;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Infrastructure.Settings.Identity;
using Zayun.Presentation.Extensions;
using Zayun.Presentation.Services;

namespace Zayun.Presentation.Settings.Authentication;

/// <summary>
/// Sign-in for the API: JWT bearer tokens made by <c>JwtTokenGenerator</c>, the Admin policy, and
/// <see cref="ICurrentUser"/> for handlers.
/// </summary>
/// <remarks>Refusals (401 / 403) are written as ProblemDetails with a <c>code</c>, like every other error.</remarks>
public static class AuthenticationSetup
{
    /// <summary>Allowance for clocks that differ a little between servers.</summary>
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    // ───────────────────────────── 1. Services ─────────────────────────────

    /// <summary>Adds authentication, authorization and the current user.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddZayunAuthentication(this IServiceCollection services)
    {
        // 1a. JWT bearer is the only sign-in scheme
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

        // 1b. Its rules come from JwtSettings (read when the first request arrives)
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtSettings>>((options, jwt) => Apply(options, jwt.Value));

        // 1c. Authorization: the admin area
        services.AddAuthorizationBuilder()
                .AddPolicy(Policies.RequireAdmin, policy => policy.RequireRole(Roles.Admin));

        // 1d. The signed-in user, for handlers
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }

    // ───────────────────────────── 2. Pipeline ─────────────────────────────

    /// <summary>Turns on authentication and authorization.</summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication UseZayunAuthentication(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    // ───────────────────────────── Helpers ─────────────────────────────

    private static void Apply(JwtBearerOptions options, JwtSettings jwt)
    {
        // Keep short claim names ("sub", "role") exactly as JwtTokenGenerator wrote them
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.CreateSigningKey(),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            ClockSkew = ClockSkew,
            NameClaimType = JwtClaimNames.UserId,
            RoleClaimType = JwtClaimNames.Role
        };

        // Refusals as ProblemDetails (by default they're empty)
        options.Events = new JwtBearerEvents
        {
            OnChallenge = WriteUnauthorizedAsync,
            OnForbidden = WriteForbiddenAsync
        };
    }

    private static Task WriteUnauthorizedAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();   // replace the default empty 401

        var error = context.AuthenticateFailure switch
        {
            null => AuthErrors.SignInRequired,
            SecurityTokenExpiredException => AuthErrors.SessionExpired,
            _ => AuthErrors.InvalidAccessToken
        };

        context.Response.Headers.WWWAuthenticate = context.AuthenticateFailure is null
            ? "Bearer"
            : "Bearer error=\"invalid_token\"";

        return error.ToProblem().ExecuteAsync(context.HttpContext);
    }

    private static Task WriteForbiddenAsync(ForbiddenContext context) =>
        AuthErrors.NotAllowed.ToProblem().ExecuteAsync(context.HttpContext);
}
