using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Quotes;

namespace Zayun.Application.Features.Quotes.Dtos;

/// <summary>Today's quote, as the website shows it.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Slug">Its web address, for the link to its own page (<c>/quotes/{slug}</c>).</param>
/// <param name="Date">The UK date it's the quote for.</param>
/// <param name="Kind">Quote, proverb or the owner's own saying.</param>
/// <param name="Text">The words, in their original language.</param>
/// <param name="Author">Who said it.</param>
/// <param name="Source">Where it comes from; null when not known.</param>
/// <param name="Language">The language when it isn't English; null for English.</param>
/// <param name="Translation">The English translation; null when it's already English.</param>
/// <param name="Series">A series, e.g. "Nature first"; null for none.</param>
/// <param name="Story">A one-line true story shown with it; null for none.</param>
/// <param name="Media">The picture (and animation), if any.</param>
public sealed record QuoteOfTheDayDto(
    Guid Id,
    string Slug,
    DateOnly Date,
    QuoteKind Kind,
    string Text,
    string Author,
    string? Source,
    string? Language,
    string? Translation,
    string? Series,
    string? Story,
    QuoteMediaDto? Media)
{
    /// <summary>Maps a quote for a date.</summary>
    /// <param name="q">The quote.</param>
    /// <param name="date">The UK date.</param>
    /// <param name="storage">Turns stored paths into web addresses.</param>
    public static QuoteOfTheDayDto From(Quote q, DateOnly date, IFileStorage storage) =>
        new(q.Id, q.Slug, date, q.Kind, q.Text, q.Author, q.Source, q.Language, q.Translation, q.Series, q.Story,
            QuoteMediaDto.From(q, storage));
}

/// <summary>A quote's picture, and its animation when there is one.</summary>
/// <param name="PictureUrl">The still picture (WebP). Also what visitors who turn off motion see.</param>
/// <param name="ThumbnailUrl">A small version of the picture, if any.</param>
/// <param name="Width">The picture's width in pixels.</param>
/// <param name="Height">The picture's height in pixels.</param>
/// <param name="AltText">The picture's description for screen readers.</param>
/// <param name="AnimationUrl">The MP4 animation; null for none.</param>
public sealed record QuoteMediaDto(
    string PictureUrl,
    string? ThumbnailUrl,
    int Width,
    int Height,
    string AltText,
    string? AnimationUrl)
{
    /// <summary>Maps a quote's media, or null when it has no picture.</summary>
    /// <param name="q">The quote.</param>
    /// <param name="storage">Turns stored paths into web addresses.</param>
    public static QuoteMediaDto? From(Quote q, IFileStorage storage) =>
        q.Picture is null
            ? null
            : new QuoteMediaDto(
                storage.GetPublicUrl(q.Picture.Path),
                q.Picture.ThumbnailPath is null ? null : storage.GetPublicUrl(q.Picture.ThumbnailPath),
                q.Picture.Dimensions.Width, q.Picture.Dimensions.Height,
                q.PictureAlt ?? q.Text,
                q.Animation is null ? null : storage.GetPublicUrl(q.Animation.Path));
}

/// <summary>A quote as Admin → Quotes shows it: everything, drafts included.</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Slug">Its web address (<c>/quotes/{slug}</c>); never changes.</param>
/// <param name="Kind">Quote, proverb or the owner's own saying.</param>
/// <param name="Text">The words, in their original language.</param>
/// <param name="Author">Who said it.</param>
/// <param name="Source">Where it comes from; null when not known.</param>
/// <param name="Language">The language when it isn't English; null for English.</param>
/// <param name="Translation">The English translation; null when it's already English.</param>
/// <param name="Series">A series, e.g. "Nature first"; null for none.</param>
/// <param name="Story">A one-line true story shown with it; null for none.</param>
/// <param name="IsSourceChecked">The wording and who said it have been checked.</param>
/// <param name="PinnedMonth">The month it's pinned to; null when not pinned.</param>
/// <param name="PinnedDay">The day it's pinned to; null when not pinned.</param>
/// <param name="DisplayOrder">Its place in the rotation.</param>
/// <param name="IsPublished">False for a draft.</param>
/// <param name="CanBeShown">True when the website may show it (published and source checked).</param>
/// <param name="Media">The picture (and animation), if any.</param>
/// <param name="SearchDescription">What Google and link previews show; null = made from the words.</param>
/// <param name="Themes">Its themes.</param>
public sealed record QuoteDto(
    Guid Id,
    string Slug,
    QuoteKind Kind,
    string Text,
    string Author,
    string? Source,
    string? Language,
    string? Translation,
    string? Series,
    string? Story,
    bool IsSourceChecked,
    int? PinnedMonth,
    int? PinnedDay,
    int DisplayOrder,
    bool IsPublished,
    bool CanBeShown,
    QuoteMediaDto? Media,
    string? SearchDescription,
    IReadOnlyList<string> Themes)
{
    /// <summary>Maps a quote.</summary>
    /// <param name="q">The quote.</param>
    /// <param name="storage">Turns stored paths into web addresses.</param>
    public static QuoteDto From(Quote q, IFileStorage storage) =>
        new(q.Id, q.Slug, q.Kind, q.Text, q.Author, q.Source, q.Language, q.Translation, q.Series, q.Story,
            q.IsSourceChecked, q.PinnedMonth, q.PinnedDay, q.DisplayOrder, q.IsPublished, QuoteRotation.IsShowable(q),
            QuoteMediaDto.From(q, storage), q.SearchDescription, q.Themes);
}

/// <summary>One day of Admin → Quotes → "Coming up": which quote shows that day and why.</summary>
/// <param name="Date">The UK date.</param>
/// <param name="Reason">Why it was chosen (pinned day, weekly series or the everyday rotation).</param>
/// <param name="QuoteId">The quote's Id; null when nothing can be shown that day.</param>
/// <param name="Text">The quote's words; null when nothing can be shown.</param>
/// <param name="Author">Who said it; null when nothing can be shown.</param>
public sealed record QuotePreviewDayDto(
    DateOnly Date,
    QuotePickReason? Reason,
    Guid? QuoteId,
    string? Text,
    string? Author);

/// <summary>A quote on the website's quote pages (<c>/quotes</c> and <c>/quotes/{slug}</c>).</summary>
/// <param name="Id">Its Id.</param>
/// <param name="Slug">Its web address.</param>
/// <param name="Kind">Quote, proverb or the owner's own saying.</param>
/// <param name="Text">The words, in their original language.</param>
/// <param name="Author">Who said it.</param>
/// <param name="Source">Where it comes from; null when not known.</param>
/// <param name="Language">The language when it isn't English; null for English.</param>
/// <param name="Translation">The English translation; null when it's already English.</param>
/// <param name="Series">A series, e.g. "Nature first"; null for none.</param>
/// <param name="Story">A one-line true story shown with it; null for none.</param>
/// <param name="LastShown">The last UK date it was the quote of the day; null when it hasn't been yet.</param>
/// <param name="Media">The picture (and animation), if any.</param>
/// <param name="SearchDescription">What Google and link previews show; null = the website makes one from the words.</param>
/// <param name="Themes">Its themes (for "related quotes" and the theme filter).</param>
public sealed record ArchivedQuoteDto(
    Guid Id,
    string Slug,
    QuoteKind Kind,
    string Text,
    string Author,
    string? Source,
    string? Language,
    string? Translation,
    string? Series,
    string? Story,
    DateOnly? LastShown,
    QuoteMediaDto? Media,
    string? SearchDescription,
    IReadOnlyList<string> Themes)
{
    /// <summary>Maps an archive entry.</summary>
    /// <param name="entry">The quote and when it was last shown.</param>
    /// <param name="storage">Turns stored paths into web addresses.</param>
    public static ArchivedQuoteDto From(ArchivedQuote entry, IFileStorage storage)
    {
        var q = entry.Quote;
        return new(q.Id, q.Slug, q.Kind, q.Text, q.Author, q.Source, q.Language, q.Translation, q.Series, q.Story,
                   entry.LastShown, QuoteMediaDto.From(q, storage), q.SearchDescription, q.Themes);
    }
}

/// <summary>The AI helper's drafts for a quote (Admin → Quotes → "Suggest with AI"). Nothing is saved: the admin
/// checks them, edits them and saves the quote as usual.</summary>
/// <param name="SearchDescription">A description for Google and link previews.</param>
/// <param name="Themes">Suggested themes (existing ones where they fit).</param>
public sealed record QuoteSuggestionDto(string SearchDescription, IReadOnlyList<string> Themes);
