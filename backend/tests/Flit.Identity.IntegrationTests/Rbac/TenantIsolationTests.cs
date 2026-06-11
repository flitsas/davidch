using System.Net.Http.Json;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
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

        await SeedTenantBAsync(tenantBRoleName);

        var client = _factory.CreateClient(new() { HandleCookies = true });
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = adminEmail, password = adminPassword });
        login.EnsureSuccessStatusCode();

        var roles = await client.GetFromJsonAsync<List<RoleSummaryResponse>>("/api/roles");
        Assert.NotNull(roles);
        Assert.Contains(roles, r => r.Name == "TenantA-Admin");
        Assert.DoesNotContain(roles, r => r.Name == tenantBRoleName);
    }

    private async Task SeedTenantBAsync(string tenantBRoleName)
    {
        await _factory.EnsureTenantSeededAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        if (await db.Tenants.AnyAsync(t => t.Slug == "tenant-b"))
        {
            return;
        }

        var tenantB = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Tenant B",
            Slug = "tenant-b",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Tenants.Add(tenantB);

        db.Roles.Add(new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantB.Id,
            Name = tenantBRoleName,
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private record RoleSummaryResponse(
        Guid Id,
        string Name,
        bool IsSystem,
        Guid? TenantId,
        DateTimeOffset CreatedAt);
}
