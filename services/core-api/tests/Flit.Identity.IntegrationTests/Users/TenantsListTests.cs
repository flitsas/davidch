using System.Net;
using System.Net.Http.Json;

namespace Flit.Identity.IntegrationTests.Users;

public class TenantsListTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public TenantsListTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task SuperAdmin_can_list_tenants()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsSuperAdminAsync();

        var tenants = await client.GetFromJsonAsync<List<TenantSummary>>("/api/tenants");
        Assert.NotNull(tenants);
        Assert.Contains(tenants!, t => t.Id == _factory.TenantId);
    }

    [Fact]
    public async Task Tenant_admin_cannot_list_tenants()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsTenantAdminAsync();

        var res = await client.GetAsync("/api/tenants");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private record TenantSummary(Guid Id, string Name, string Slug);
}
