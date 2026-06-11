using System.Text.Json.Serialization;
using Flit.Identity.Auth;
using Flit.Identity.Infrastructure.Audit;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Rbac;
using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Users;

public sealed class SetUserRolesHandler(
    IdentityDbContext db,
    SessionRevocationService revocation,
    RoleConflictAnalyzer conflictAnalyzer,
    AuditService audit)
{
    public async Task<IResult> HandleAsync(
        Guid userId,
        SetUserRolesRequest req,
        CurrentUser admin,
        CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .SingleOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return Results.Json(new { code = ApiErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound);
        }

        if (!admin.IsSuperAdmin && user.TenantId != admin.TenantId)
        {
            return Results.Json(new { code = ApiErrorCodes.Forbidden }, statusCode: StatusCodes.Status403Forbidden);
        }

        var roleIds = req.RoleIds.Distinct().ToArray();
        if (roleIds.Length == 0)
        {
            return Results.Json(
                new { code = ApiErrorCodes.ValidationError, message = "At least one role is required." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var proposedRoles = await db.Roles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id) && (admin.IsSuperAdmin || r.TenantId == admin.TenantId))
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .ToListAsync(ct);

        if (proposedRoles.Count != roleIds.Length)
        {
            return Results.Json(
                new { code = ApiErrorCodes.ValidationError, message = "One or more role IDs are invalid." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToArray();
        var currentRoles = currentRoleIds.Length == 0
            ? []
            : await db.Roles
                .AsNoTracking()
                .Where(r => currentRoleIds.Contains(r.Id))
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ToListAsync(ct);

        var currentGrants = ToGrants(currentRoles);
        var proposedGrants = ToGrants(proposedRoles);

        var warnings = conflictAnalyzer.Analyze(currentGrants, proposedGrants)
            .Concat(conflictAnalyzer.AnalyzeRoles(ToRoleGrantSets(proposedRoles)))
            .ToList();

        if (warnings.Count > 0 && !req.Confirm)
        {
            return Results.Ok(new
            {
                warnings = warnings.Select(w => new
                {
                    type = w.Type,
                    message = w.Message,
                    role_id = w.RoleId,
                    other_role_id = w.OtherRoleId,
                    permission_key = w.PermissionKey
                }),
                pending = true
            });
        }

        db.UserRoles.RemoveRange(user.UserRoles);
        foreach (var roleId in roleIds)
        {
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        await db.SaveChangesAsync(ct);
        await revocation.RevokeAllSessionsAsync(user.Id, ct);
        await audit.LogPrivilegeChangeAsync(
            admin.Id,
            AuditActions.UserRolesChanged,
            "user",
            user.Id,
            new { role_ids = roleIds },
            ct);

        return Results.NoContent();
    }

    private static IReadOnlyList<PermissionGrant> ToGrants(IEnumerable<Role> roles) =>
        roles
            .SelectMany(r => r.RolePermissions)
            .Select(rp => new PermissionGrant(rp.Permission.Key, rp.Scope))
            .GroupBy(g => (g.Key, g.Scope))
            .Select(g => g.First())
            .ToList();

    private static IReadOnlyList<RoleGrantSet> ToRoleGrantSets(IEnumerable<Role> roles) =>
        roles
            .Select(r => new RoleGrantSet(
                r.Id,
                r.Name,
                r.RolePermissions
                    .Select(rp => new PermissionGrant(rp.Permission.Key, rp.Scope))
                    .ToList()))
            .ToList();
}

public record SetUserRolesRequest(
    [property: JsonPropertyName("role_ids")] IReadOnlyList<Guid> RoleIds,
    [property: JsonPropertyName("confirm")] bool Confirm = false);
