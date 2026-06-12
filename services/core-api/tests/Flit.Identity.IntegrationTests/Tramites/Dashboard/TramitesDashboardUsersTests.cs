using System.Net.Http.Json;
using Flit.Procedures.Runtime.Dashboard;

namespace Flit.Identity.IntegrationTests.Tramites.Dashboard;

public class TramitesDashboardUsersTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;
    private static HttpClient? _superAdminClient;
    private static readonly SemaphoreSlim LoginGate = new(1, 1);

    public TramitesDashboardUsersTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Top_users_includes_tenant_admin_after_seeding()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var superAdmin = await SuperAdminAsync();
        var tenantAdmin = await _factory.LoginAsTenantAdminAsync();
        await TramitesDashboardTestHelper.SeedThreeCategoryInstancesAsync(superAdmin, tenantAdmin);

        var response = await tenantAdmin.GetAsync("/api/v1/tramites/dashboard/users/top");
        response.EnsureSuccessStatusCode();
        var top = await response.Content.ReadFromJsonAsync<DashboardUsersTopResponse>();
        Assert.NotNull(top);
        Assert.NotEmpty(top.Items);
        Assert.True(top.Items[0].Count >= 3);
    }

    [Fact]
    public async Task User_stats_returns_zero_for_user_without_tramites()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var tenantAdmin = await _factory.LoginAsTenantAdminAsync();

        var response = await tenantAdmin.GetAsync(
            $"/api/v1/tramites/dashboard/users/{Guid.NewGuid()}/stats");
        response.EnsureSuccessStatusCode();
        var stats = await response.Content.ReadFromJsonAsync<DashboardUserStatsResponse>();
        Assert.NotNull(stats);
        Assert.Equal(0, stats.Count);
    }

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
}
