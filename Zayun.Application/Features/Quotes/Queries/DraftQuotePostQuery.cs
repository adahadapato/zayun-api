using MediatR;
using Zayun.Application.Features.Quotes.Dtos;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Quotes;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Features.Quotes.Queries;

/// <summary>
/// Admin → Quotes → "Draft a post": a ready-to-copy post for LinkedIn and a shorter one for WhatsApp or X, for a SAVED
/// quote (it needs its web address). Saves nothing, posts nothing. Never cached.
/// </summary>
/// <param name="Id">The quote.</param>
/// <param name="Today">Today's UK date (to tell whether the quote's page is on the website yet).</param>
/// <param name="WebsiteBaseUrl">The website's address, e.g. "https://zayun.net", for the link.</param>
public sealed record DraftQuotePostQuery(Guid Id, DateOnly Today, string WebsiteBaseUrl)
    : IRequest<Result<QuotePostDraftDto>>;

/// <summary>Handles <see cref="DraftQuotePostQuery"/>.</summary>
/// <remarks>
/// <para>The AI writes only the intros and the hashtags. The quote's words, who said it, the source, the translation
/// and the link are put in here from the saved quote, exactly as they are, so a signed saying or a proverb can't be
/// reworded.</para>
/// <para>The hashtags are tidied (letters and digits only, at most <see cref="MaxHashtags"/>), and the short post gets
/// only the first <see cref="MaxShortHashtags"/>, to stay near X's 280 characters.</para>
/// </remarks>
public sealed class DraftQuotePostQueryHandler(IQuoteRepository quotes, IPostAdvisor advisor)
    : IRequestHandler<DraftQuotePostQuery, Result<QuotePostDraftDto>>
{
    /// <summary>Most hashtags on the LinkedIn post.</summary>
    public const int MaxHashtags = 5;

    /// <summary>Most hashtags on the short post.</summary>
    public const int MaxShortHashtags = 2;

    private const int LongIntroMaxLength = 400;
    private const int ShortIntroMaxLength = 140;

    /// <inheritdoc />
    public async Task<Result<QuotePostDraftDto>> Handle(DraftQuotePostQuery query, CancellationToken ct)
    {
        var quote = await quotes.GetByIdAsync(query.Id, ct);
        if (quote is null) return QuoteErrors.NotFound;
        if (!advisor.IsAvailable) return QuoteErrors.PostAiUnavailable;

        var draft = await advisor.DraftAsync(Describe(quote), ct);
        if (draft is null) return QuoteErrors.PostAiNoAnswer;

        var hashtags = CleanHashtags(draft.Hashtags);
        var link = $"{query.WebsiteBaseUrl.TrimEnd('/')}/quotes/{quote.Slug}";

        // Is the quote's page on the website yet? (Drafts, unchecked quotes, and pinned / series quotes before their
        // day aren't: the link would show "Page not found" until then.)
        var published = await quotes.GetListAsync(publishedOnly: true, ct);
        var isOnWebsite = QuoteArchive.Build(published, query.Today).Any(e => e.Quote.Id == quote.Id);

        return new QuotePostDraftDto(
            LinkedIn: LinkedInPost(quote, Shorten(draft.LongIntro, LongIntroMaxLength), link, hashtags),
            Short: ShortPost(quote, Shorten(draft.ShortIntro, ShortIntroMaxLength), link,
                             hashtags.Take(MaxShortHashtags).ToList()),
            Hashtags: hashtags,
            Link: link,
            IsOnWebsite: isOnWebsite);
    }

    /// <summary>The quote, as the AI reads it: only its own saved fields.</summary>
    private static PostDraftRequest Describe(Quote q)
    {
        var what = q.Kind switch
        {
            QuoteKind.Proverb => "a traditional proverb",
            QuoteKind.OwnSaying => "the owner's own saying",
            _ => "a quote"
        };

        List<PostFact> facts =
        [
            new("Words (added by the website, don't repeat them)", q.Text),
            new("Who said it", q.Author)
        ];
        if (!string.IsNullOrWhiteSpace(q.Source)) facts.Add(new("Source", q.Source));
        if (!string.IsNullOrWhiteSpace(q.Language)) facts.Add(new("Language", q.Language));
        if (!string.IsNullOrWhiteSpace(q.Translation)) facts.Add(new("English translation", q.Translation));
        if (!string.IsNullOrWhiteSpace(q.Series)) facts.Add(new("Series", q.Series));
        if (!string.IsNullOrWhiteSpace(q.Story)) facts.Add(new("The true story shown with it", q.Story));

        return new PostDraftRequest(what, facts, q.Themes);
    }

    /// <summary>The LinkedIn post: intro, the quote, who said it, the link, hashtags.</summary>
    private static string LinkedInPost(Quote q, string intro, string link, IReadOnlyList<string> hashtags)
    {
        var lines = new List<string> { intro, "", $"“{q.Text}”" };
        if (!string.IsNullOrWhiteSpace(q.Translation)) lines.Add($"In English: “{q.Translation}”");
        lines.Add($"— {Attribution(q)}");
        lines.Add("");
        lines.Add(link);
        if (hashtags.Count > 0)
        {
            lines.Add("");
            lines.Add(Tags(hashtags));
        }
        return string.Join("\n", lines);
    }

    /// <summary>The short post (WhatsApp, X): the quote and who said it, the intro, the link, a few hashtags.</summary>
    private static string ShortPost(Quote q, string intro, string link, IReadOnlyList<string> hashtags)
    {
        var lines = new List<string> { $"“{q.Text}” — {q.Author}" };
        if (!string.IsNullOrWhiteSpace(q.Translation)) lines.Add($"In English: “{q.Translation}”");
        lines.Add(intro);
        lines.Add(link);
        if (hashtags.Count > 0) lines.Add(Tags(hashtags));
        return string.Join("\n", lines);
    }

    private static string Attribution(Quote q) =>
        string.IsNullOrWhiteSpace(q.Source) ? q.Author : $"{q.Author}, {q.Source}";

    private static string Tags(IEnumerable<string> hashtags) => string.Join(' ', hashtags.Select(t => "#" + t));

    /// <summary>Letters and digits only, starting with a letter, 2–40 characters, no repeats (any case), at most
    /// <see cref="MaxHashtags"/>.</summary>
    private static List<string> CleanHashtags(IEnumerable<string> hashtags) => hashtags
        .Select(t => new string((t ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()))
        .Where(t => t.Length is >= 2 and <= 40 && char.IsLetter(t[0]))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(MaxHashtags)
        .ToList();

    /// <summary>Trims the text and, when too long, cuts it at a word and ends it with "…".</summary>
    private static string Shorten(string text, int maxLength)
    {
        var clean = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (clean.Length <= maxLength) return clean;

        var cut = clean[..(maxLength - 1)];
        var lastSpace = cut.LastIndexOf(' ');
        return (lastSpace > maxLength / 2 ? cut[..lastSpace] : cut).TrimEnd(',', ';', ':', '.', ' ') + "…";
    }
}
