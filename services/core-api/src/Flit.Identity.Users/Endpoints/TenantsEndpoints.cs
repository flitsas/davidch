using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Rbac;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Users.Endpoints;

public static class TenantsEndpoints
{
    public static IEndpointRouteBuilder MapTenantsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tenants", ListTenantsAsync);
        return app;
    }

    private static async Task<IResult> ListTenantsAsync(IdentityDbContext db, HttpContext http, CancellationToken ct)
    {
        var user = CurrentUserFactory.FromPrincipal(http.User);
        if (user is null)
        {
            return Results.Json(
                new { code = ApiErrorCodes.TokenExpired },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!user.IsSuperAdmin)
        {
            return Results.Json(
                new { code = ApiErrorCodes.Forbidden },
                statusCode: StatusCodes.Status403Forbidden);
        }

        var tenants = await db.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new TenantSummaryResponse(t.Id, t.Name, t.Slug))
            .ToListAsync(ct);

        return Results.Ok(tenants);
    }

    private record TenantSummaryResponse(Guid Id, string Name, string Slug);
}
