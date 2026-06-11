using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Shared.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Rbac.Endpoints;

public static class PermissionsEndpoints
{
    public static IEndpointRouteBuilder MapPermissionsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/permissions", async (IdentityDbContext db, CancellationToken ct) =>
        {
            var permissions = await db.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Key)
                .Select(p => new PermissionResponse(
                    p.Id,
                    p.Key,
                    p.Type.ToString(),
                    p.Module,
                    p.Description))
                .ToListAsync(ct);

            return Results.Ok(permissions);
        })
        .RequirePermission("roles:read", PermissionScope.Tenant);

        return app;
    }

    private record PermissionResponse(
        Guid Id,
        string Key,
        string Type,
        string? Module,
        string Description);
}
