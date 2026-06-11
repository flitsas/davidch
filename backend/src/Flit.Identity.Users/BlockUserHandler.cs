using Flit.Identity.Auth;
using Flit.Identity.Infrastructure.Audit;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Domain;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Users;

public sealed class BlockUserHandler(
    IdentityDbContext db,
    SessionRevocationService revocation,
    AuditService audit)
{
    public async Task<IResult> HandleAsync(Guid userId, CurrentUser admin, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Results.Json(new { code = ApiErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound);
        }

        if (!admin.IsSuperAdmin && user.TenantId != admin.TenantId)
        {
            return Results.Json(new { code = ApiErrorCodes.Forbidden }, statusCode: StatusCodes.Status403Forbidden);
        }

        if (user.Status == UserStatus.Blocked)
        {
            return Results.NoContent();
        }

        user.Status = UserStatus.Blocked;
        await db.SaveChangesAsync(ct);
        await revocation.RevokeAllSessionsAsync(user.Id, ct);
        await audit.LogPrivilegeChangeAsync(
            admin.Id,
            AuditActions.UserBlocked,
            "user",
            user.Id,
            ct: ct);

        return Results.NoContent();
    }
}
