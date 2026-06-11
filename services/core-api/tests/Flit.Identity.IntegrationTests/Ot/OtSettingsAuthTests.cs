using System.Net;
using System.Net.Http.Json;

namespace Flit.Identity.IntegrationTests.Ot;

public class OtSettingsAuthTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public OtSettingsAuthTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Tenant_admin_with_tramites_update_can_access_settings()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsTenantAdminAsync();

        var response = await client.GetAsync("/api/v1/ot/settings/config/integration");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Tenant_operator_without_tramites_update_gets_forbidden()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsTenantOperatorAsync();

        var response = await client.GetAsync("/api/v1/ot/settings/config/integration");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("FORBIDDEN", body?.Code);
    }

    private sealed record ErrorResponse(string Code);
}
