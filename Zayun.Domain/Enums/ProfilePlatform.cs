namespace Zayun.Domain.Enums;

/// <summary>
/// Which site an online profile is on, so the website can show the right icon. A site not listed here uses
/// <see cref="Other"/> (a plain link icon); new ones can be added without changing stored data.
/// </summary>
public enum ProfilePlatform
{
    // Research
    Orcid = 1,
    ResearchGate = 2,
    GoogleScholar = 3,
    Arxiv = 4,
    Ieee = 5,
    Scopus = 6,
    SemanticScholar = 7,
    Dblp = 8,
    Zenodo = 9,
    Academia = 10,

    // Engineering and social
    LinkedIn = 20,
    GitHub = 21,
    HuggingFace = 22,
    Kaggle = 23,
    StackOverflow = 24,
    X = 25,
    YouTube = 26,
    Medium = 27,

    // Anything else
    Website = 90,
    Other = 99
}
