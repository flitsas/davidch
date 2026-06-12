using System.Net;
using System.Net.Http.Json;

namespace Flit.Identity.IntegrationTests.Tramites;

public class TramitesAuthTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public TramitesAuthTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Tenant_operator_without_tramites_read_gets_forbidden_on_index()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsTenantOperatorAsync();

        var response = await client.GetAsync("/api/v1/tramites/index");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Tenant_admin_with_tramites_read_can_access_index()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsTenantAdminAsync();

        var response = await client.GetAsync("/api/v1/tramites/index");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed record ErrorResponse(string Code);
}
