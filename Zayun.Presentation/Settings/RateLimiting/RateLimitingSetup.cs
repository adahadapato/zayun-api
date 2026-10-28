using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Zayun.Presentation.Settings.RateLimiting;

/// <summary>
/// Limits on the public endpoints robots like most: the Contact form and sign-in. Per connection (IP address),
/// in memory: a restart resets them.
/// </summary>
public static class RateLimitingSetup
{
    /// <summary>The Contact form's policy.</summary>
    public const string ContactPolicy = "contact";

    /// <summary>Messages allowed per window.</summary>
    public const int ContactPermitLimit = 5;

    /// <summary>The Contact form's window.</summary>
    public static readonly TimeSpan ContactWindow = TimeSpan.FromMinutes(10);

    /// <summary>The sign-in policy (on top of Identity's lockout).</summary>
    public const string SignInPolicy = "sign-in";

    /// <summary>Sign-in attempts allowed per window.</summary>
    public const int SignInPermitLimit = 10;

    /// <summary>The sign-in window.</summary>
    public static readonly TimeSpan SignInWindow = TimeSpan.FromMinutes(5);

    /// <summary>Adds the rate limiter and its policies.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddZayunRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(ContactPolicy, context => FixedWindow(context, ContactPermitLimit, ContactWindow));
            options.AddPolicy(SignInPolicy, context => FixedWindow(context, SignInPermitLimit, SignInWindow));

            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too many requests",
                    status = StatusCodes.Status429TooManyRequests,
                    code = "RateLimit.TooManyRequests",
                    detail = "That's a lot of requests in a short time. Please wait a few minutes and try again."
                }, ct);
            };
        });

        return services;
    }

    private static RateLimitPartition<string> FixedWindow(HttpContext context, int permits, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 });
}
