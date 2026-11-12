namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for quotes. Nothing here is thrown.</summary>
public static class QuoteErrors
{
    /// <summary>No quote with this Id.</summary>
    public static readonly Error NotFound = Error.NotFound("Quote.NotFound", "That quote wasn't found.");

    /// <summary>Nothing to show today: no quote is both published and source-checked.</summary>
    public static readonly Error NoneToday =
        Error.NotFound("Quote.NoneToday", "There's no quote to show today.");
}
