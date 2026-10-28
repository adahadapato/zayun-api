using System.Security.Claims;
using Zayun.Domain.Interfaces;
using Zayun.Infrastructure.Settings.Identity;

namespace Zayun.Presentation.Services;

/// <summary>
/// Who is making the current request, read from the access token that JWT authentication has already checked.
/// Handlers use this instead of a user Id in the request body, so nobody can act as someone else.
/// </summary>
/// <remarks>
/// <para>No token, or an invalid one: <see cref="UserId"/> is null and <see cref="IsAuthenticated"/> is false.</para>
/// <para>Roles come from the token, so a role given or removed takes effect at the next token refresh (at most
/// <c>Jwt:ExpiryMinutes</c> later).</para>
/// </remarks>
internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    /// <summary>The checked token's claims for this request; null outside a request.</summary>
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(JwtClaimNames.UserId), out var id) ? id : null;

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true && UserId is not null;

    /// <inheritdoc />
    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}