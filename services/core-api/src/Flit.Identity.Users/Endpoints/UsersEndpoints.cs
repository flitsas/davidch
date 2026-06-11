using System.Text.Json.Serialization;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Tenancy;
using Flit.Identity.Rbac;
using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Users.Endpoints;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users");

        group.MapPost("/invite", InviteUserAsync)
            .RequirePermission("users:create", PermissionScope.Tenant);

        group.MapGet("/", ListUsersAsync)
            .RequirePermission("users:read", PermissionScope.Tenant);

        group.MapPut("/{id:guid}/roles", SetUserRolesAsync)
            .RequirePermission("users:update", PermissionScope.Tenant);

        group.MapPost("/{id:guid}/force-reset", ForceResetAsync)
            .RequirePermission("users:update", PermissionScope.Tenant);

        group.MapPost("/{id:guid}/block", BlockUserAsync)
            .RequirePermission("users:update", PermissionScope.Tenant);

        return app;
    }

    private static Task<IResult> SetUserRolesAsync(
        Guid id,
        SetUserRolesRequest req,
        SetUserRolesHandler handler,
        HttpContext ctx,
        CancellationToken ct)
    {
        var admin = (CurrentUser)ctx.Items["CurrentUser"]!;
        return handler.HandleAsync(id, req, admin, ct);
    }

    private static Task<IResult> BlockUserAsync(
        Guid id,
        BlockUserHandler handler,
        HttpContext ctx,
        CancellationToken ct)
    {
        var admin = (CurrentUser)ctx.Items["CurrentUser"]!;
        return handler.HandleAsync(id, admin, ct);
    }

    private static Task<IResult> ForceResetAsync(
        Guid id,
        ForceResetHandler handler,
        HttpContext ctx,
        CancellationToken ct)
    {
        var admin = (CurrentUser)ctx.Items["CurrentUser"]!;
        return handler.HandleAsync(id, admin, ct);
    }

    private static Task<IResult> InviteUserAsync(
        InviteRequest req,
        InviteUserHandler handler,
        HttpContext ctx,
        CancellationToken ct)
    {
        var inviter = (CurrentUser)ctx.Items["CurrentUser"]!;
        return handler.HandleAsync(req, inviter, ct);
    }

    private static async Task<IResult> ListUsersAsync(
        IdentityDbContext db,
        ITenantContext tenant,
        CancellationToken ct)
    {
        var query = db.Users.AsNoTracking();
        if (tenant.CurrentTenantId is { } tenantId)
        {
            query = query.Where(u => u.TenantId == tenantId);
        }

        var users = await query
            .OrderBy(u => u.Email)
            .Select(u => new UserSummaryResponse(
                u.Id,
                u.Email,
                u.Status.ToString(),
                u.TenantId,
                u.CreatedAt,
                u.ActivatedAt,
                u.UserRoles.Select(ur => ur.RoleId).ToArray()))
            .ToListAsync(ct);

        return Results.Ok(users);
    }

    private record UserSummaryResponse(
        Guid Id,
        string Email,
        string Status,
        Guid? TenantId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ActivatedAt,
        [property: JsonPropertyName("role_ids")] Guid[] RoleIds);
}
