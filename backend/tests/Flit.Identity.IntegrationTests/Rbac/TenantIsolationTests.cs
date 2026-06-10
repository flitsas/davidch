using System.Net.Http.Json;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Rbac;

public class TenantIsolationTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public TenantIsolationTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Tenant_admin_cannot_see_other_tenant_roles()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        const string adminEmail = "admin@tenant-a.com";
        const string adminPassword = "SecurePass!123";
        const string tenantBRoleName = "TenantB-Operator";

        await SeedTwoTenantsAsync(adminEmail, adminPassword, tenantBRoleName);

        var client = _factory.CreateClient(new() { HandleCookies = true });
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = adminEmail, password = adminPassword });
        login.EnsureSuccessStatusCode();

        var roles = await client.GetFromJsonAsync<List<RoleSummaryResponse>>("/api/roles");
        Assert.NotNull(roles);
        Assert.Contains(roles, r => r.Name == "TenantA-Admin");
        Assert.DoesNotContain(roles, r => r.Name == tenantBRoleName);
    }

    private async Task SeedTwoTenantsAsync(string adminEmail, string adminPassword, string tenantBRoleName)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var tenantA = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Tenant A",
            Slug = "tenant-a",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var tenantB = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Tenant B",
            Slug = "tenant-b",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Tenants.AddRange(tenantA, tenantB);

        var rolesRead = await db.Permissions.SingleAsync(p => p.Key == "roles:read");

        var tenantAAdminRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA.Id,
            Name = "TenantA-Admin",
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Roles.Add(tenantAAdminRole);
        db.RolePermissions.Add(new RolePermission
        {
            RoleId = tenantAAdminRole.Id,
            PermissionId = rolesRead.Id,
            Scope = PermissionScope.Tenant
        });

        var tenantBRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantB.Id,
            Name = tenantBRoleName,
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Roles.Add(tenantBRole);

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            PasswordHash = hasher.Hash(adminPassword),
            TenantId = tenantA.Id,
            Status = UserStatus.Active,
            TokenVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ActivatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(adminUser);
        db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = tenantAAdminRole.Id });

        await db.SaveChangesAsync();
    }

    private record RoleSummaryResponse(
        Guid Id,
        string Name,
        bool IsSystem,
        Guid? TenantId,
        DateTimeOffset CreatedAt);
}
