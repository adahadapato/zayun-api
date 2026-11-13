using MediatR;
using Zayun.Application.Common.Caching;
using Zayun.Application.Common.Content;
using Zayun.Application.Features.Quotes.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Quotes;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Quotes.Commands;

/// <summary>The fields Admin → Quotes types in, shared by <see cref="CreateQuoteCommand"/> and
/// <see cref="UpdateQuoteCommand"/> (and their validator), so the rules are written once.</summary>
public interface IQuoteFields
{
    /// <summary>Quote, proverb or the owner's own saying.</summary>
    QuoteKind Kind { get; }

    /// <summary>The words, in their original language.</summary>
    string Text { get; }

    /// <summary>Who said it.</summary>
    string Author { get; }

    /// <summary>Where it comes from (optional).</summary>
    string? Source { get; }

    /// <summary>The language when it isn't English (optional).</summary>
    string? Language { get; }

    /// <summary>The English translation (optional; needs <see cref="Language"/>).</summary>
    string? Translation { get; }

    /// <summary>A series, e.g. "Nature first" (optional). Series quotes are shown on Sundays.</summary>
    string? Series { get; }

    /// <summary>A one-line true story shown with it (optional).</summary>
    string? Story { get; }

    /// <summary>The wording and who said it have been checked.</summary>
    bool IsSourceChecked { get; }

    /// <summary>The month it's pinned to (optional; with <see cref="PinnedDay"/>).</summary>
    int? PinnedMonth { get; }

    /// <summary>The day it's pinned to (optional; with <see cref="PinnedMonth"/>).</summary>
    int? PinnedDay { get; }

    /// <summary>What Google and link previews show (optional; made from the words when empty).</summary>
    string? SearchDescription { get; }

    /// <summary>Its themes, e.g. "Nature", "Flight" (optional).</summary>
    IReadOnlyList<string>? Themes { get; }
}

/// <summary>Turns the typed-in fields into the Domain's <see cref="QuoteDetails"/>.</summary>
internal static class QuoteFields
{
    /// <summary>The details.</summary>
    /// <param name="c">The command.</param>
    public static QuoteDetails ToDetails(this IQuoteFields c) =>
        new(c.Kind, c.Text, c.Author, c.Source, c.Language, c.Translation, c.Series, c.Story, c.IsSourceChecked,
            c.PinnedMonth, c.PinnedDay, c.SearchDescription, c.Themes);
}

/// <summary>Admin → New quote. It goes to the end of the rotation, and gets its web address (<see cref="QuoteSlugs"/>),
/// which then never changes.</summary>
/// <param name="Kind">Quote, proverb or the owner's own saying.</param>
/// <param name="Text">The words.</param>
/// <param name="Author">Who said it.</param>
/// <param name="Source">Where it comes from.</param>
/// <param name="Language">The language when it isn't English.</param>
/// <param name="Translation">The English translation.</param>
/// <param name="Series">A series, e.g. "Nature first".</param>
/// <param name="Story">A one-line true story.</param>
/// <param name="IsSourceChecked">The wording and who said it have been checked.</param>
/// <param name="PinnedMonth">The month it's pinned to.</param>
/// <param name="PinnedDay">The day it's pinned to.</param>
/// <param name="SearchDescription">What Google and link previews show.</param>
/// <param name="Themes">Its themes.</param>
/// <param name="IsPublished">Publish it straight away, or keep it as a draft.</param>
[InvalidatesCache(CacheTags.Quotes)]
public sealed record CreateQuoteCommand(
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
    string? SearchDescription,
    IReadOnlyList<string>? Themes,
    bool IsPublished) : IRequest<Result<QuoteDto>>, IQuoteFields;

/// <summary>Handles <see cref="CreateQuoteCommand"/>.</summary>
public sealed class CreateQuoteCommandHandler(IQuoteRepository quotes, IUnitOfWork unitOfWork, IFileStorage storage)
    : IRequestHandler<CreateQuoteCommand, Result<QuoteDto>>
{
    /// <inheritdoc />
    public async Task<Result<QuoteDto>> Handle(CreateQuoteCommand c, CancellationToken ct)
    {
        var slug = QuoteSlugs.Make(c.Author, c.Text, c.Translation, await quotes.GetUsedSlugsAsync(ct));
        var quote = Quote.Create(c.ToDetails(), slug, await quotes.GetNextDisplayOrderAsync(ct), c.IsPublished);

        quotes.Add(quote);
        await unitOfWork.SaveChangesAsync(ct);
        return QuoteDto.From(quote, storage);
    }
}

/// <summary>Admin → Edit quote.</summary>
/// <param name="Id">The quote.</param>
/// <param name="Kind">Quote, proverb or the owner's own saying.</param>
/// <param name="Text">The words.</param>
/// <param name="Author">Who said it.</param>
/// <param name="Source">Where it comes from.</param>
/// <param name="Language">The language when it isn't English.</param>
/// <param name="Translation">The English translation.</param>
/// <param name="Series">A series, e.g. "Nature first".</param>
/// <param name="Story">A one-line true story.</param>
/// <param name="IsSourceChecked">The wording and who said it have been checked.</param>
/// <param name="PinnedMonth">The month it's pinned to.</param>
/// <param name="PinnedDay">The day it's pinned to.</param>
/// <param name="SearchDescription">What Google and link previews show.</param>
/// <param name="Themes">Its themes.</param>
[InvalidatesCache(CacheTags.Quotes)]
public sealed record UpdateQuoteCommand(
    Guid Id,
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
    string? SearchDescription,
    IReadOnlyList<string>? Themes) : IRequest<Result<QuoteDto>>, IQuoteFields;

/// <summary>Handles <see cref="UpdateQuoteCommand"/>.</summary>
public sealed class UpdateQuoteCommandHandler(IQuoteRepository quotes, IUnitOfWork unitOfWork, IFileStorage storage)
    : IRequestHandler<UpdateQuoteCommand, Result<QuoteDto>>
{
    /// <inheritdoc />
    public async Task<Result<QuoteDto>> Handle(UpdateQuoteCommand c, CancellationToken ct)
    {
        var quote = await quotes.GetByIdAsync(c.Id, ct);
        if (quote is null) return QuoteErrors.NotFound;

        quote.Update(c.ToDetails());
        await unitOfWork.SaveChangesAsync(ct);
        return QuoteDto.From(quote, storage);
    }
}

/// <summary>Admin → Quotes: publish (true) or hide (false) a quote. A published quote is only shown once its source
/// is checked.</summary>
/// <param name="Id">The quote.</param>
/// <param name="IsPublished">True to publish it.</param>
[InvalidatesCache(CacheTags.Quotes)]
public sealed record SetQuotePublishedCommand(Guid Id, bool IsPublished) : IRequest<Result>;

/// <summary>Handles <see cref="SetQuotePublishedCommand"/>.</summary>
public sealed class SetQuotePublishedCommandHandler(IQuoteRepository quotes, IUnitOfWork unitOfWork)
    : IRequestHandler<SetQuotePublishedCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(SetQuotePublishedCommand c, CancellationToken ct) =>
        ContentActions.SetPublishedAsync(quotes, unitOfWork, c.Id, c.IsPublished, QuoteErrors.NotFound, ct);
}

/// <summary>Admin → Quotes: delete a quote.</summary>
/// <param name="Id">The quote.</param>
[InvalidatesCache(CacheTags.Quotes)]
public sealed record DeleteQuoteCommand(Guid Id) : IRequest<Result>;

/// <summary>Handles <see cref="DeleteQuoteCommand"/>.</summary>
public sealed class DeleteQuoteCommandHandler(IQuoteRepository quotes, IUnitOfWork unitOfWork, TimeProvider clock)
    : IRequestHandler<DeleteQuoteCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(DeleteQuoteCommand c, CancellationToken ct) =>
        ContentActions.DeleteAsync(quotes, unitOfWork, clock, c.Id, QuoteErrors.NotFound, ct);
}

/// <summary>Admin → Quotes: reorder the rotation. Send every quote's Id in the new order.</summary>
/// <param name="Ids">Every quote's Id, each once.</param>
[InvalidatesCache(CacheTags.Quotes)]
public sealed record ReorderQuotesCommand(IReadOnlyList<Guid> Ids) : IRequest<Result>;

/// <summary>Handles <see cref="ReorderQuotesCommand"/>.</summary>
public sealed class ReorderQuotesCommandHandler(IQuoteRepository quotes, IUnitOfWork unitOfWork)
    : IRequestHandler<ReorderQuotesCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ReorderQuotesCommand c, CancellationToken ct) =>
        ContentActions.ReorderAsync(quotes, unitOfWork, c.Ids, ct);
}
