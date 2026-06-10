using System.Text.Json.Serialization;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Infrastructure.Tenancy;
using Flit.Identity.Shared.Domain;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Rbac.Endpoints;

public static class RolesEndpoints
{
    public static IEndpointRouteBuilder MapRolesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles");

        group.MapGet("/", ListRolesAsync)
            .RequirePermission("roles:read", PermissionScope.Tenant);

        group.MapPost("/", CreateRoleAsync)
            .RequirePermission("roles:create", PermissionScope.Tenant);

        group.MapGet("/{id:guid}", GetRoleAsync)
            .RequirePermission("roles:read", PermissionScope.Tenant);

        group.MapPut("/{id:guid}", UpdateRoleAsync)
            .RequirePermission("roles:update", PermissionScope.Tenant);

        group.MapPut("/{id:guid}/permissions", SetRolePermissionsAsync)
            .RequirePermission("roles:update", PermissionScope.Tenant);

        group.MapDelete("/{id:guid}", DeleteRoleAsync)
            .RequirePermission("roles:delete", PermissionScope.Tenant);

        return app;
    }

    private static async Task<IResult> ListRolesAsync(IdentityDbContext db, CancellationToken ct)
    {
        var roles = await db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleSummaryResponse(r.Id, r.Name, r.IsSystem, r.TenantId, r.CreatedAt))
            .ToListAsync(ct);

        return Results.Ok(roles);
    }

    private static async Task<IResult> CreateRoleAsync(
        CreateRoleRequest req,
        IdentityDbContext db,
        ITenantContext tenant,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return Results.Json(
                new { code = ApiErrorCodes.ValidationError, message = "Name is required." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (tenant.CurrentTenantId is null)
        {
            return Results.Json(
                new { code = ApiErrorCodes.ValidationError, message = "Tenant context is required." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.CurrentTenantId,
            Name = req.Name.Trim(),
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/roles/{role.Id}",
            new RoleSummaryResponse(role.Id, role.Name, role.IsSystem, role.TenantId, role.CreatedAt));
    }

    private static async Task<IResult> GetRoleAsync(
        Guid id,
        IdentityDbContext db,
        CancellationToken ct)
    {
        var role = await db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .SingleOrDefaultAsync(r => r.Id == id, ct);

        if (role is null)
        {
            return Results.Json(
                new { code = ApiErrorCodes.NotFound },
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Ok(new RoleDetailResponse(
            role.Id,
            role.Name,
            role.IsSystem,
            role.TenantId,
            role.CreatedAt,
            role.RolePermissions
                .Select(rp => new RolePermissionResponse(rp.PermissionId, rp.Permission.Key, rp.Scope))
                .ToList()));
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid id,
        UpdateRoleRequest req,
        IdentityDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return Results.Json(
                new { code = ApiErrorCodes.ValidationError, message = "Name is required." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var role = await db.Roles.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (role is null)
        {
            return Results.Json(
                new { code = ApiErrorCodes.NotFound },
                statusCode: StatusCodes.Status404NotFound);
        }

        if (role.IsSystem)
        {
            return Results.Json(
                new { code = ApiErrorCodes.Forbidden, message = "System roles cannot be modified." },
                statusCode: StatusCodes.Status403Forbidden);
        }

        role.Name = req.Name.Trim();
        await db.SaveChangesAsync(ct);

        return Results.Ok(new RoleSummaryResponse(role.Id, role.Name, role.IsSystem, role.TenantId, role.CreatedAt));
    }

    private static async Task<IResult> SetRolePermissionsAsync(
        Guid id,
        SetRolePermissionsRequest req,
        IdentityDbContext db,
        CancellationToken ct)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .SingleOrDefaultAsync(r => r.Id == id, ct);

        if (role is null)
        {
            return Results.Json(
                new { code = ApiErrorCodes.NotFound },
                statusCode: StatusCodes.Status404NotFound);
        }

        if (role.IsSystem)
        {
            return Results.Json(
                new { code = ApiErrorCodes.Forbidden, message = "System roles cannot be modified." },
                statusCode: StatusCodes.Status403Forbidden);
        }

        var permissionIds = req.Permissions.Select(p => p.PermissionId).Distinct().ToArray();
        var validPermissionIds = await db.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (validPermissionIds.Count != permissionIds.Length)
        {
            return Results.Json(
                new { code = ApiErrorCodes.ValidationError, message = "One or more permission IDs are invalid." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        db.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var entry in req.Permissions)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = entry.PermissionId,
                Scope = entry.Scope
            });
        }

        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteRoleAsync(
        Guid id,
        IdentityDbContext db,
        CancellationToken ct)
    {
        var role = await db.Roles
            .Include(r => r.UserRoles)
            .SingleOrDefaultAsync(r => r.Id == id, ct);

        if (role is null)
        {
            return Results.Json(
                new { code = ApiErrorCodes.NotFound },
                statusCode: StatusCodes.Status404NotFound);
        }

        if (role.IsSystem)
        {
            return Results.Json(
                new { code = ApiErrorCodes.Forbidden, message = "System roles cannot be deleted." },
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (role.UserRoles.Count > 0)
        {
            return Results.Json(
                new { code = ApiErrorCodes.RoleHasUsers, affected_users = role.UserRoles.Count },
                statusCode: StatusCodes.Status409Conflict);
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    public record CreateRoleRequest(string Name);
    public record UpdateRoleRequest(string Name);
    public record SetRolePermissionsRequest(IReadOnlyList<RolePermissionEntry> Permissions);
    public record RolePermissionEntry(
        [property: JsonPropertyName("permission_id")] Guid PermissionId,
        PermissionScope Scope);

    private record RoleSummaryResponse(
        Guid Id,
        string Name,
        bool IsSystem,
        Guid? TenantId,
        DateTimeOffset CreatedAt);

    private record RoleDetailResponse(
        Guid Id,
        string Name,
        bool IsSystem,
        Guid? TenantId,
        DateTimeOffset CreatedAt,
        IReadOnlyList<RolePermissionResponse> Permissions);

    private record RolePermissionResponse(Guid PermissionId, string Key, PermissionScope Scope);
}
