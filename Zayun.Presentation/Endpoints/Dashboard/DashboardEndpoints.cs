using MediatR;
using Zayun.Application.Features.Dashboard.Queries;
using Zayun.Domain.Constants;
using Zayun.Presentation.Extensions;

namespace Zayun.Presentation.Endpoints.Dashboard;

/// <summary><c>/api/admin/overview</c>: the admin dashboard's numbers.</summary>
public static class DashboardEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/admin/overview", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAdminOverviewQuery(), ct)).ToHttp())
            .RequireAuthorization(Policies.RequireAdmin)
            .WithTags("Admin: Dashboard")
            .WithName("GetAdminOverview")
            .WithSummary("How much content there is (total and published) and how many new messages")
            .Produces<AdminOverviewDto>();

        return api;
    }
}
