namespace Zayun.Presentation.Settings.OpenApi;

/// <summary>
/// The <c>OpenApi</c> section: the API description document and the Swagger page.
/// Keys: <c>AppSettings.OpenApi</c>.
/// </summary>
public sealed class OpenApiSettings
{
    /// <summary>The placeholder in <see cref="DocumentPath"/> that becomes <see cref="Version"/>.</summary>
    public const string DocumentNamePlaceholder = "{documentName}";

    /// <summary>Publish the document and the Swagger page. Off in production by default.</summary>
    public bool Enabled { get; set; }

    /// <summary>Title at the top of the page.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Document name and version, e.g. "v1".</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Text under the title.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Where the JSON document is served, e.g. "/openapi/{documentName}.json".</summary>
    public string DocumentPath { get; set; } = string.Empty;

    /// <summary>Address of the Swagger page, e.g. "swagger".</summary>
    public string UiPath { get; set; } = string.Empty;

    /// <summary>The developer shown under the title.</summary>
    public OpenApiContactSettings Contact { get; set; } = new();

    /// <summary>The document's actual address, e.g. "/openapi/v1.json" (what the Swagger page loads).</summary>
    public string DocumentUrl => DocumentPath.Replace(DocumentNamePlaceholder, Version);
}