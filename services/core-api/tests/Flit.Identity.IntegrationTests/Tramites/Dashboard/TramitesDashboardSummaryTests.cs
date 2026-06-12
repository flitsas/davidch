using System.Net.Http.Json;
using Flit.Procedures.Runtime.Dashboard;

namespace Flit.Identity.IntegrationTests.Tramites.Dashboard;

public class TramitesDashboardSummaryTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;
    private static HttpClient? _superAdminClient;
    private static readonly SemaphoreSlim LoginGate = new(1, 1);

    public TramitesDashboardSummaryTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Summary_returns_correct_category_counts()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var superAdmin = await SuperAdminAsync();
        var tenantAdmin = await _factory.LoginAsTenantAdminAsync();
        await TramitesDashboardTestHelper.SeedThreeCategoryInstancesAsync(superAdmin, tenantAdmin);

        var response = await tenantAdmin.GetAsync("/api/v1/tramites/dashboard/summary");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>();
        Assert.NotNull(summary);
        Assert.True(summary.Total >= 3);

        var matriculas = summary.Categories.Single(c => c.Key == ProcedureCategoryClassifier.Matriculas);
        var traspasos = summary.Categories.Single(c => c.Key == ProcedureCategoryClassifier.Traspasos);
        var otros = summary.Categories.Single(c => c.Key == ProcedureCategoryClassifier.Otros);
        Assert.True(matriculas.Count >= 1);
        Assert.True(traspasos.Count >= 1);
        Assert.True(otros.Count >= 1);
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
