using System.Net;
using System.Net.Http.Json;
using Flit.Companies.Infrastructure.Persistence;
using Flit.Companies.Shared.Domain;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Companies;

public class CompaniesCrudTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public CompaniesCrudTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_link_mode_provisions_company_with_defaults()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsSuperAdminAsync();

        Guid linkTenantId;
        using (var tenantScope = _factory.Services.CreateScope())
        {
            var identityDb = tenantScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            linkTenantId = Guid.NewGuid();
            identityDb.Tenants.Add(new Tenant
            {
                Id = linkTenantId,
                Name = "Company Link Test Tenant",
                Slug = $"co-link-{Guid.NewGuid():N}"[..20],
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await identityDb.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/v1/admin/companies", new
        {
            mode = "link",
            tenant_id = linkTenantId,
            nit = $"900{Guid.NewGuid():N}"[..12],
            legal_name = "Linked Company SAS",
            status = "Active"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        Assert.NotNull(created);

        using var scope = _factory.Services.CreateScope();
        var companiesDb = scope.ServiceProvider.GetRequiredService<CompaniesDbContext>();
        var matricula = await companiesDb.CompanyMatriculaConfigs
            .SingleAsync(c => c.CompanyId == created!.Id);
        Assert.False(matricula.AllowNewVehicleFiling);

        var matrixCount = await companiesDb.CompanyTrafficAuthorityMatrix
            .CountAsync(m => m.CompanyId == created.Id);
        Assert.True(matrixCount >= 6);
    }

    [Fact]
    public async Task Create_create_mode_provisions_tenant_and_company()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await _factory.LoginAsSuperAdminAsync();
        var slug = $"co-{Guid.NewGuid():N}"[..16];

        var response = await client.PostAsJsonAsync("/api/v1/admin/companies", new
        {
            mode = "create",
            nit = $"900{Guid.NewGuid():N}"[..12],
            legal_name = "New B2B SAS",
            slug,
            status = "Active"
        });

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        Assert.NotNull(created);

        using var scope = _factory.Services.CreateScope();
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var tenant = await identityDb.Tenants.SingleAsync(t => t.Id == created!.TenantId);
        Assert.Equal(slug, tenant.Slug);
    }

    [Fact]
    public async Task Patch_status_suspends_tenant()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await _factory.LoginAsSuperAdminAsync();
        var slug = $"suspend-{Guid.NewGuid():N}"[..20];
        var create = await client.PostAsJsonAsync("/api/v1/admin/companies", new
        {
            mode = "create",
            nit = $"900{Guid.NewGuid():N}"[..12],
            legal_name = "Suspend Test SAS",
            slug,
            status = "Active"
        });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatedResponse>();

        var patch = await client.PatchAsJsonAsync(
            $"/api/v1/admin/companies/{created!.Id}/status",
            new { status = "Suspended" });
        patch.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var tenant = await identityDb.Tenants.SingleAsync(t => t.Id == created.TenantId);
        Assert.False(tenant.IsActive);
    }

    private sealed record CreatedResponse(Guid Id, Guid TenantId);
}
