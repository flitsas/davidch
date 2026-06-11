using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds a demo tenant with roles and users for local Docker / E2E flows.
/// Idempotent: skips when any tenant already exists.
/// </summary>
public static class DevTenantSeeder
{
    public const string TenantAdminEmail = "admin@tenant-a.com";
    public const string TenantAdminPassword = "SecurePass!123";
    public const string TenantOperatorEmail = "operator@tenant-a.com";
    public const string TenantOperatorPassword = "SecurePass!123";
    public const string TenantSlug = "tenant-a";

    private static readonly string[] TenantAdminPermissionKeys =
    [
        "users:create", "users:read", "users:update",
        "roles:create", "roles:read", "roles:update", "roles:delete",
        "tramites:update",
    ];

    public static async Task SeedAsync(
        IdentityDbContext db,
        IPasswordHasher hasher,
        CancellationToken ct = default)
    {
        if (await db.Tenants.AnyAsync(ct))
        {
            await EnsureTenantAdminPermissionsAsync(db, ct);
            return;
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Tenant A",
            Slug = TenantSlug,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Tenants.Add(tenant);

        var permissions = await db.Permissions
            .Where(p => TenantAdminPermissionKeys.Contains(p.Key))
            .ToDictionaryAsync(p => p.Key, ct);

        var adminRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "TenantA-Admin",
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Roles.Add(adminRole);

        foreach (var key in TenantAdminPermissionKeys)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permissions[key].Id,
                Scope = PermissionScope.Tenant
            });
        }

        var operatorRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "TenantA-Operator",
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Roles.Add(operatorRole);

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = TenantAdminEmail,
            PasswordHash = hasher.Hash(TenantAdminPassword),
            TenantId = tenant.Id,
            Status = UserStatus.Active,
            TokenVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ActivatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(adminUser);
        db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });

        var operatorUser = new User
        {
            Id = Guid.NewGuid(),
            Email = TenantOperatorEmail,
            PasswordHash = hasher.Hash(TenantOperatorPassword),
            TenantId = tenant.Id,
            Status = UserStatus.Active,
            TokenVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ActivatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(operatorUser);
        db.UserRoles.Add(new UserRole { UserId = operatorUser.Id, RoleId = operatorRole.Id });

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Backfills new dev permissions when the demo tenant was seeded before a key existed.
    /// </summary>
    private static async Task EnsureTenantAdminPermissionsAsync(IdentityDbContext db, CancellationToken ct)
    {
        var tenant = await db.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Slug == TenantSlug, ct);

        if (tenant is null)
        {
            return;
        }

        var adminRole = await db.Roles
            .SingleOrDefaultAsync(r => r.TenantId == tenant.Id && r.Name == "TenantA-Admin", ct);

        if (adminRole is null)
        {
            return;
        }

        var permissions = await db.Permissions
            .Where(p => TenantAdminPermissionKeys.Contains(p.Key))
            .ToDictionaryAsync(p => p.Key, ct);

        var assigned = await db.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToHashSetAsync(ct);

        var changed = false;
        foreach (var key in TenantAdminPermissionKeys)
        {
            if (!permissions.TryGetValue(key, out var permission) || assigned.Contains(permission.Id))
            {
                continue;
            }

            db.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id,
                Scope = PermissionScope.Tenant,
            });
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
