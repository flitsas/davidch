using System.Net;

namespace Flit.Identity.IntegrationTests.Tramites.Dashboard;

public class TramitesDashboardAuthTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public TramitesDashboardAuthTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Tenant_operator_without_users_read_gets_forbidden()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsTenantOperatorAsync();
        var response = await client.GetAsync("/api/v1/tramites/dashboard/summary");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Tenant_admin_with_users_read_can_access_summary()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsTenantAdminAsync();
        var response = await client.GetAsync("/api/v1/tramites/dashboard/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
