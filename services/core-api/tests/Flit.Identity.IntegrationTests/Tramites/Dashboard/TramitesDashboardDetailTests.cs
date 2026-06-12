using System.Net.Http.Json;
using Flit.Procedures.Runtime.Dashboard;

namespace Flit.Identity.IntegrationTests.Tramites.Dashboard;

public class TramitesDashboardDetailTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;
    private static HttpClient? _superAdminClient;
    private static readonly SemaphoreSlim LoginGate = new(1, 1);

    public TramitesDashboardDetailTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Detail_filters_by_category()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var superAdmin = await SuperAdminAsync();
        var tenantAdmin = await _factory.LoginAsTenantAdminAsync();
        var seeded = await TramitesDashboardTestHelper.SeedThreeCategoryInstancesAsync(superAdmin, tenantAdmin);

        var response = await tenantAdmin.GetAsync(
            $"/api/v1/tramites/dashboard/detail?category={ProcedureCategoryClassifier.Traspasos}");
        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<DashboardDetailResponse>();
        Assert.NotNull(detail);
        Assert.Contains(detail.Items, i => i.Plate == seeded.TraspasoPlate);
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
