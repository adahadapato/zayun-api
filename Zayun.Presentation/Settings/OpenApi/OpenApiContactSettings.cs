namespace Zayun.Presentation.Settings.OpenApi;

/// <summary>
/// The <c>OpenApi:Contact</c> sub-section: who builds and looks after the API, shown under the Swagger title.
/// Keys: <c>AppSettings.OpenApi.Contact</c>.
/// </summary>
public sealed class OpenApiContactSettings
{
    /// <summary>Developer's name. Empty = not shown.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Developer's email. Empty = not shown.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Developer's website or profile (absolute URL). Empty or invalid = not shown.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>True when at least one value is set.</summary>
    public bool HasAny =>
        !string.IsNullOrWhiteSpace(Name) || !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(Url);
}