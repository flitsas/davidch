using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Shared.Auth;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Auth;

public sealed class PermissionResolver(IdentityDbContext db)
{
    public async Task<IReadOnlyList<PermissionGrant>> ResolveAsync(User user, CancellationToken ct)
    {
        if (user.TenantId is null)
        {
            return await db.RolePermissions
                .Where(rp => rp.Role.IsSystem)
                .Select(rp => new PermissionGrant(rp.Permission.Key, rp.Scope))
                .Distinct()
                .ToListAsync(ct);
        }

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToArray();
        var grants = await db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => new PermissionGrant(rp.Permission.Key, rp.Scope))
            .ToListAsync(ct);

        return grants
            .GroupBy(g => (g.Key, g.Scope))
            .Select(g => g.First())
            .ToList();
    }
}
