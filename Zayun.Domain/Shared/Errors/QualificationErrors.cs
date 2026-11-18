namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for qualifications. Nothing here is thrown.</summary>
public static class QualificationErrors
{
    /// <summary>No qualification with this Id.</summary>
    public static readonly Error NotFound =
        Error.NotFound("Qualification.NotFound", "That qualification wasn't found.");
}
