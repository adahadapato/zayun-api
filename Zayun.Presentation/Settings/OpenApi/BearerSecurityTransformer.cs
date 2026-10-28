using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Zayun.Presentation.Settings.OpenApi;

/// <summary>
/// Adds the "Authorize" button (JWT Bearer) to Swagger, and the lock icon to endpoints that need sign-in.
/// </summary>
/// <remarks>
/// <para><b>Document:</b> registers the Bearer scheme once. Paste the access token (without "Bearer ") into the
/// Authorize box, and Swagger sends it on every locked endpoint.</para>
/// <para><b>Operations:</b> an endpoint is locked when it (or its group) has <c>RequireAuthorization</c> /
/// <c>[Authorize]</c> and not <c>AllowAnonymous</c>.</para>
/// </remarks>
internal sealed class BearerSecurityTransformer : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    /// <summary>The scheme's name in the document; matches the JWT Bearer authentication scheme name.</summary>
    private const string SchemeName = "Bearer";

    /// <inheritdoc />
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
                               CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the access token from sign-in (without the word \"Bearer\")."
        };

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
                               CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        var needsSignIn = metadata.OfType<IAuthorizeData>().Any()
                       && !metadata.OfType<IAllowAnonymous>().Any();
        if (!needsSignIn) return Task.CompletedTask;

        // 1. Lock icon: send the token
        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, context.Document)] = []
        });

        // 2. Document the refusals, so Swagger doesn't call them "Undocumented"
        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Not signed in, or the session has expired (code: Auth.SignInRequired / Auth.SessionExpired / Auth.InvalidAccessToken)"
        });
        operation.Responses.TryAdd("403", new OpenApiResponse
        {
            Description = "Signed in, but not allowed (code: Auth.NotAllowed)"
        });

        return Task.CompletedTask;
    }
}