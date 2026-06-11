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

    public static async Task SeedAsync(
        IdentityDbContext db,
        IPasswordHasher hasher,
        CancellationToken ct = default)
    {
        if (await db.Tenants.AnyAsync(ct))
        {
            return;
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Tenant A",
            Slug = "tenant-a",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Tenants.Add(tenant);

        var permissionKeys = new[]
        {
            "users:create", "users:read", "users:update",
            "roles:create", "roles:read", "roles:update", "roles:delete"
        };
        var permissions = await db.Permissions
            .Where(p => permissionKeys.Contains(p.Key))
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

        foreach (var key in permissionKeys)
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

        await db.SaveChangesAsync(ct);
    }
}
