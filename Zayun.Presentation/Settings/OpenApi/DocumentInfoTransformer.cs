using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Zayun.Presentation.Settings.OpenApi;

/// <summary>Fills the document's title, version, description and developer contact from <see cref="OpenApiSettings"/>.</summary>
internal sealed class DocumentInfoTransformer(IOptions<OpenApiSettings> options) : IOpenApiDocumentTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
                               CancellationToken cancellationToken)
    {
        var settings = options.Value;

        document.Info = new OpenApiInfo
        {
            Title = settings.Title,
            Version = settings.Version,
            Description = settings.Description,
            Contact = BuildContact(settings.Contact)
        };

        return Task.CompletedTask;
    }

    /// <summary>The contact block, or null when nothing is set; an invalid URL is left out.</summary>
    private static OpenApiContact? BuildContact(OpenApiContactSettings contact)
    {
        if (!contact.HasAny) return null;

        return new OpenApiContact
        {
            Name = NullIfBlank(contact.Name),
            Email = NullIfBlank(contact.Email),
            Url = Uri.TryCreate(contact.Url, UriKind.Absolute, out var url) ? url : null
        };
    }

    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}