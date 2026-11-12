namespace Zayun.Domain.Enums;

/// <summary>
/// What kind of saying a quote is, so the website can show it the right way (a proverb shows its language and
/// translation; the owner's own sayings are signed with his name).
/// </summary>
/// <remarks>New values are only ever added at the end; stored numbers never change.</remarks>
public enum QuoteKind
{
    /// <summary>Someone else's words, e.g. Ada Lovelace or Richard Feynman.</summary>
    Quote = 1,

    /// <summary>A traditional proverb, e.g. an Annang or Efik proverb with its English translation.</summary>
    Proverb = 2,

    /// <summary>The owner's own saying, e.g. the "Nature first" series.</summary>
    OwnSaying = 3
}
