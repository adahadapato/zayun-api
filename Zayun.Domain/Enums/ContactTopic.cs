namespace Zayun.Domain.Enums;

/// <summary>What a Contact message is about (the form's "Topic"), so the inbox can be sorted.</summary>
public enum ContactTopic
{
    /// <summary>Building software together, or a project to discuss.</summary>
    Project = 1,

    /// <summary>Research collaboration or questions about a paper.</summary>
    Research = 2,

    /// <summary>A job or consulting opportunity.</summary>
    Opportunity = 3,

    /// <summary>An invitation to speak or present.</summary>
    Speaking = 4,

    /// <summary>Anything else, including feedback.</summary>
    Other = 5
}
