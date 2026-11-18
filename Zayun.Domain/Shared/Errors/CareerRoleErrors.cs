namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for career roles. Nothing here is thrown.</summary>
public static class CareerRoleErrors
{
    /// <summary>No role with this Id.</summary>
    public static readonly Error NotFound = Error.NotFound("CareerRole.NotFound", "That role wasn't found.");
}
