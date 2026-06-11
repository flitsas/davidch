using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Flit.Identity.Infrastructure.Persistence.Seed;

public static class IdentityDbSeeder
{
    public static async Task SeedAsync(
        IdentityDbContext db,
        IConfiguration config,
        IPasswordHasher hasher,
        CancellationToken ct = default)
    {
        if (await db.Permissions.AnyAsync(ct))
        {
            return;
        }

        var modules = new[] { "users", "roles", "tramites" };
        var permissions = new List<Permission>();

        foreach (var module in modules)
        {
            foreach (var action in new[] { "create", "read", "update", "delete" })
            {
                permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Key = $"{module}:{action}",
                    Type = PermissionType.Crud,
                    Module = module,
                    Description = $"{module} {action}"
                });
            }
        }

        permissions.Add(new Permission
        {
            Id = Guid.NewGuid(),
            Key = "generar_consolidado",
            Type = PermissionType.Ui,
            Module = null,
            Description = "Generar consolidado"
        });

        db.Permissions.AddRange(permissions);

        var superRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = null,
            Name = "SuperAdmin",
            IsSystem = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Roles.Add(superRole);

        foreach (var permission in permissions)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = superRole.Id,
                PermissionId = permission.Id,
                Scope = PermissionScope.Global
            });
        }

        var email = config["Identity:BootstrapEmail"] ?? "super@flit.local";
        var password = config["Identity:BootstrapPassword"] ?? "ChangeMe!123";
        var superUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = hasher.Hash(password),
            Status = UserStatus.Active,
            TokenVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ActivatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(superUser);
        db.UserRoles.Add(new UserRole { UserId = superUser.Id, RoleId = superRole.Id });

        await db.SaveChangesAsync(ct);
    }
}
