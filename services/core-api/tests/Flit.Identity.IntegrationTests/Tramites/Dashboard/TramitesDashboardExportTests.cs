using System.Net;
using System.Net.Http.Json;
using Flit.Procedures.Runtime.Dashboard;

namespace Flit.Identity.IntegrationTests.Tramites.Dashboard;

public class TramitesDashboardExportTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;
    private static HttpClient? _superAdminClient;
    private static readonly SemaphoreSlim LoginGate = new(1, 1);

    public TramitesDashboardExportTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Export_returns_xlsx_content_type()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var superAdmin = await SuperAdminAsync();
        var tenantAdmin = await _factory.LoginAsTenantAdminAsync();
        await TramitesDashboardTestHelper.SeedThreeCategoryInstancesAsync(superAdmin, tenantAdmin);

        var response = await tenantAdmin.GetAsync(
            $"/api/v1/tramites/dashboard/export?category={ProcedureCategoryClassifier.Traspasos}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Content.Headers.ContentLength > 0);
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
