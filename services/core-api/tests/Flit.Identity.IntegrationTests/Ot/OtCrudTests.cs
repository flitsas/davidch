using System.Net;
using System.Net.Http.Json;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.OT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Ot;

public class OtCrudTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;
    private static HttpClient? _superAdminClient;
    private static readonly SemaphoreSlim LoginGate = new(1, 1);

    public OtCrudTests(IdentityWebApplicationFactory factory) => _factory = factory;

    private async Task<HttpClient> SuperAdminAsync()
    {
        if (_superAdminClient is not null)
        {
            return _superAdminClient;
        }

        await LoginGate.WaitAsync();
        try
        {
            _superAdminClient ??= await _factory.LoginAsSuperAdminAsync();
            return _superAdminClient;
        }
        finally
        {
            LoginGate.Release();
        }
    }

    [Fact]
    public async Task Create_link_mode_provisions_ot_with_default_order_items()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await SuperAdminAsync();
        var divipol = $"11001{Guid.NewGuid():N}"[..8];

        Guid linkTenantId;
        using (var tenantScope = _factory.Services.CreateScope())
        {
            var identityDb = tenantScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            linkTenantId = Guid.NewGuid();
            identityDb.Tenants.Add(new Tenant
            {
                Id = linkTenantId,
                Name = "OT Link Test Tenant",
                Slug = $"link-{Guid.NewGuid():N}"[..20],
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await identityDb.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/v1/admin/ot", new
        {
            mode = "link",
            tenant_id = linkTenantId,
            divipol_code = divipol,
            display_name = "OT Bogotá Test",
            status = "Active"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        Assert.NotNull(created);

        using var scope = _factory.Services.CreateScope();
        var otDb = scope.ServiceProvider.GetRequiredService<OtDbContext>();
        var orderCount = await otDb.OtDocumentOrderItems
            .CountAsync(i => i.TenantId == created!.TenantId);
        Assert.True(orderCount >= 5);
    }

    [Fact]
    public async Task Create_create_mode_provisions_tenant_and_ot()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await SuperAdminAsync();
        var slug = $"ot-{Guid.NewGuid():N}"[..16];
        var divipol = $"76001{Guid.NewGuid():N}"[..8];

        var response = await client.PostAsJsonAsync("/api/v1/admin/ot", new
        {
            mode = "create",
            divipol_code = divipol,
            display_name = "OT Cali Test",
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
    public async Task Create_rejects_duplicate_divipol()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await SuperAdminAsync();
        var divipol = $"05001{Guid.NewGuid():N}"[..8];
        var slug1 = $"ot-a-{Guid.NewGuid():N}"[..16];
        var slug2 = $"ot-b-{Guid.NewGuid():N}"[..16];

        var first = await client.PostAsJsonAsync("/api/v1/admin/ot", new
        {
            mode = "create",
            divipol_code = divipol,
            display_name = "OT Medellín A",
            slug = slug1,
            status = "Active"
        });
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/v1/admin/ot", new
        {
            mode = "create",
            divipol_code = divipol,
            display_name = "OT Medellín B",
            slug = slug2,
            status = "Active"
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_duplicate_tenant()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await SuperAdminAsync();

        Guid tenantId;
        using (var scope = _factory.Services.CreateScope())
        {
            var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var tenant = new Flit.Identity.Infrastructure.Persistence.Entities.Tenant
            {
                Id = Guid.NewGuid(),
                Name = "OT Duplicate Tenant Test",
                Slug = $"ot-dup-{Guid.NewGuid():N}"[..20],
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            identityDb.Tenants.Add(tenant);
            await identityDb.SaveChangesAsync();
            tenantId = tenant.Id;
        }

        var divipol1 = $"13001{Guid.NewGuid():N}"[..8];
        var divipol2 = $"13002{Guid.NewGuid():N}"[..8];

        var first = await client.PostAsJsonAsync("/api/v1/admin/ot", new
        {
            mode = "link",
            tenant_id = tenantId,
            divipol_code = divipol1,
            display_name = "OT Cartagena",
            status = "Active"
        });
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/v1/admin/ot", new
        {
            mode = "link",
            tenant_id = tenantId,
            divipol_code = divipol2,
            display_name = "OT Cartagena Duplicate",
            status = "Active"
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Patch_status_suspends_tenant()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await SuperAdminAsync();
        var slug = $"suspend-ot-{Guid.NewGuid():N}"[..20];
        var divipol = $"68001{Guid.NewGuid():N}"[..8];

        var create = await client.PostAsJsonAsync("/api/v1/admin/ot", new
        {
            mode = "create",
            divipol_code = divipol,
            display_name = "OT Suspend Test",
            slug,
            status = "Active"
        });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatedResponse>();

        var patch = await client.PatchAsJsonAsync(
            $"/api/v1/admin/ot/{created!.Id}/status",
            new { status = "Suspended" });
        patch.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var tenant = await identityDb.Tenants.SingleAsync(t => t.Id == created.TenantId);
        Assert.False(tenant.IsActive);
    }

    [Fact]
    public async Task Get_detail_returns_ot_profile()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await SuperAdminAsync();
        var slug = $"detail-ot-{Guid.NewGuid():N}"[..20];
        var divipol = $"08001{Guid.NewGuid():N}"[..8];

        var create = await client.PostAsJsonAsync("/api/v1/admin/ot", new
        {
            mode = "create",
            divipol_code = divipol,
            display_name = "OT Barranquilla",
            slug,
            status = "Active"
        });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatedResponse>();

        var detail = await client.GetAsync($"/api/v1/admin/ot/{created!.Id}");
        detail.EnsureSuccessStatusCode();
        var body = await detail.Content.ReadFromJsonAsync<DetailResponse>();
        Assert.Equal(divipol, body!.DivipolCode);
        Assert.Equal("OT Barranquilla", body.DisplayName);
    }

    private sealed record CreatedResponse(Guid Id, Guid TenantId);

    private sealed record DetailResponse(string DivipolCode, string DisplayName);
}
