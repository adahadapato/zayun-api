using Zayun.Presentation.Endpoints.Auth;
using Zayun.Presentation.Endpoints.Awards;
using Zayun.Presentation.Endpoints.Contact;
using Zayun.Presentation.Endpoints.Dashboard;
using Zayun.Presentation.Endpoints.Documents;
using Zayun.Presentation.Endpoints.Experience;
using Zayun.Presentation.Endpoints.Presentations;
using Zayun.Presentation.Endpoints.Profiles;
using Zayun.Presentation.Endpoints.Projects;
using Zayun.Presentation.Endpoints.Publications;
using Zayun.Presentation.Endpoints.SiteImages;
using Zayun.Presentation.Endpoints.Skills;
using Zayun.Presentation.Endpoints.Tags;

namespace Zayun.Presentation.Extensions;

/// <summary>Every route of the API, in one place.</summary>
public static class EndpointExtensions
{
    /// <summary>Maps all endpoint groups under <c>/api</c>, plus <c>/private-files</c> and <c>/health</c>.</summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication MapZayunEndpoints(this WebApplication app)
    {
        // 1. API groups
        var api = app.MapGroup("/api");

        api.MapAuthEndpoints();              // /api/auth
        api.MapProjectEndpoints();           // /api/projects        + /api/admin/projects
        api.MapPublicationEndpoints();       // /api/publications    + /api/admin/publications
        api.MapPresentationEndpoints();      // /api/presentations   + /api/admin/presentations
        api.MapExperienceEndpoints();        // /api/experience      + /api/admin/experience
        api.MapSkillEndpoints();             // /api/skills          + /api/admin/skills
        api.MapAwardEndpoints();             // /api/awards          + /api/admin/awards
        api.MapTagEndpoints();               //                        /api/admin/tags
        api.MapProfileEndpoints();           // /api/profiles        + /api/admin/profiles
        api.MapSiteImageEndpoints();         // /api/site-images     + /api/admin/site-images
        api.MapContactEndpoints();           // /api/contact         + /api/admin/contact-messages
        api.MapDashboardEndpoints();         //                        /api/admin/overview
        api.MapDocumentEndpoints();          //                        /api/admin/documents

        // 2. Private documents through signed links, e.g. /private-files/…?name=…&expires=…&sig=… (not in Swagger)
        app.MapPrivateFiles();

        // 3. Health (not in Swagger)
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
           .ExcludeFromDescription();

        return app;
    }
}
