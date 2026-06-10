using System.Net;
using System.Net.Http.Json;

namespace Flit.Identity.IntegrationTests.Auth;

public class SessionEvictionTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public SessionEvictionTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Role_change_revokes_existing_access_token()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var admin = await _factory.LoginAsTenantAdminAsync();
        var target = await _factory.CreateActiveUserAsync();

        var cookies = await _factory.LoginAndGetCookiesAsync(target.Email);

        var assignRoles = await admin.PutAsJsonAsync($"/api/users/{target.Id}/roles",
            new { role_ids = new[] { _factory.TenantAdminRoleId }, confirm = true });
        assignRoles.EnsureSuccessStatusCode();

        var res = await _factory.SendWithCookiesAsync("/api/auth/me", cookies);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.True(res.Headers.Contains("X-Session-Revoked"));
        Assert.Equal("SESSION_REVOKED", await res.ReadErrorCodeAsync());
    }
}
