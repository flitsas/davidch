using System.Text.Json.Serialization;
using Flit.Identity.Auth;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Users;

public sealed class SetUserRolesHandler(IdentityDbContext db, SessionRevocationService revocation)
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

        var validRoleCount = await db.Roles
            .Where(r => roleIds.Contains(r.Id) && (admin.IsSuperAdmin || r.TenantId == admin.TenantId))
            .CountAsync(ct);

        if (validRoleCount != roleIds.Length)
        {
            return Results.Json(
                new { code = ApiErrorCodes.ValidationError, message = "One or more role IDs are invalid." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        db.UserRoles.RemoveRange(user.UserRoles);
        foreach (var roleId in roleIds)
        {
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        await db.SaveChangesAsync(ct);
        await revocation.RevokeAllSessionsAsync(user.Id, ct);

        return Results.NoContent();
    }
}

public record SetUserRolesRequest(
    [property: JsonPropertyName("role_ids")] IReadOnlyList<Guid> RoleIds,
    [property: JsonPropertyName("confirm")] bool Confirm = false);
