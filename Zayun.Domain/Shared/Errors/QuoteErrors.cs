using Zayun.Domain.ValueObjects;

namespace Zayun.Domain.Shared.Errors;

/// <summary>Every business-rule failure for quotes. Nothing here is thrown.</summary>
public static class QuoteErrors
{
    /// <summary>No quote with this Id.</summary>
    public static readonly Error NotFound = Error.NotFound("Quote.NotFound", "That quote wasn't found.");

    /// <summary>Nothing to show today: no quote is both published and source-checked.</summary>
    public static readonly Error NoneToday =
        Error.NotFound("Quote.NoneToday", "There's no quote to show today.");

    /// <summary>"Suggest with AI" while the AI is switched off (no key, or <c>Ai:Enabled</c> false).</summary>
    public static readonly Error AiUnavailable = Error.Conflict(
        "Quote.AiUnavailable", "The AI helper is switched off, so please write the description and themes yourself.");

    /// <summary>The AI didn't answer in time or gave no usable answer.</summary>
    public static readonly Error AiNoAnswer = Error.Conflict(
        "Quote.AiNoAnswer", "The AI helper didn't answer this time. Please try again in a moment, or write them yourself.");

    /// <summary>Removing a picture that isn't there.</summary>
    public static readonly Error NoPicture = Error.NotFound("Quote.NoPicture", "This quote has no picture to remove.");

    /// <summary>Removing an animation that isn't there.</summary>
    public static readonly Error NoAnimation =
        Error.NotFound("Quote.NoAnimation", "This quote has no animation to remove.");

    /// <summary>An animation was uploaded before the picture.</summary>
    public static readonly Error AnimationNeedsPicture = Error.Validation(
        "Quote.AnimationNeedsPicture",
        "Please add the still picture first: it's what visitors who turn off motion see instead of the animation.");

    /// <summary>The animation file is empty.</summary>
    public static readonly Error AnimationEmpty = Error.Validation("Quote.AnimationEmpty", "That file is empty.");

    /// <summary>The animation isn't an MP4 video.</summary>
    public static readonly Error AnimationNotMp4 = Error.Validation(
        "Quote.AnimationNotMp4", "Please upload the animation as an MP4 video.");

    /// <summary>The animation is too large.</summary>
    public static readonly Error AnimationTooLarge = Error.Validation(
        "Quote.AnimationTooLarge",
        $"Please keep the animation under {AnimationFile.MaxFileSizeBytes / (1024 * 1024)} MB.");
}
