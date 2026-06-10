using System.Net;
using System.Net.Http.Json;
using Flit.Identity.Auth;

namespace Flit.Identity.IntegrationTests.Auth;

public class LoginRefreshLogoutTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public LoginRefreshLogoutTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_sets_httpOnly_cookies_and_me_returns_user()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = _factory.CreateClient(new() { HandleCookies = true });

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "super@flit.local", password = "ChangeMe!123" });
        login.EnsureSuccessStatusCode();

        Assert.Contains(login.Headers.GetValues("Set-Cookie"), h => h.StartsWith("flit_access=", StringComparison.Ordinal));
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), h => h.StartsWith("flit_refresh=", StringComparison.Ordinal));

        var me = await client.GetAsync("/api/auth/me");
        me.EnsureSuccessStatusCode();
        var body = await me.Content.ReadFromJsonAsync<MeResponse>();
        Assert.Equal("super@flit.local", body!.Email);
        Assert.True(body.IsSuperAdmin);
        Assert.Contains("SuperAdmin", body.Roles);
    }

    [Fact]
    public async Task Refresh_rotates_cookies()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = _factory.CreateClient(new() { HandleCookies = true });

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "super@flit.local", password = "ChangeMe!123" });
        login.EnsureSuccessStatusCode();

        var refreshBefore = GetCookieValue(login.Headers.GetValues("Set-Cookie"), "flit_refresh");

        var refresh = await client.PostAsync("/api/auth/refresh", null);
        refresh.EnsureSuccessStatusCode();

        var refreshAfter = GetCookieValue(refresh.Headers.GetValues("Set-Cookie"), "flit_refresh");
        Assert.NotEqual(refreshBefore, refreshAfter);

        var me = await client.GetAsync("/api/auth/me");
        me.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Logout_clears_session()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = _factory.CreateClient(new() { HandleCookies = true });

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "super@flit.local", password = "ChangeMe!123" });
        login.EnsureSuccessStatusCode();

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    private static string? GetCookieValue(IEnumerable<string> setCookieHeaders, string name)
    {
        foreach (var header in setCookieHeaders)
        {
            if (header.StartsWith($"{name}=", StringComparison.Ordinal))
            {
                return header.Split(';')[0].Split('=', 2)[1];
            }
        }

        return null;
    }
}
