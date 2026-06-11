using System.Net;
using System.Net.Http.Json;
using Flit.Companies.Infrastructure.Persistence;
using Flit.Companies.Shared.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Companies;

public class CompaniesConfigTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public CompaniesConfigTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Matricula_config_round_trip()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var companyId = await CreateCompanyAsync();
        var client = await _factory.LoginAsSuperAdminAsync();

        var put = await client.PutAsJsonAsync(
            $"/api/v1/admin/companies/{companyId}/config/matricula",
            new { allowNewVehicleFiling = true, allowMiscProcedures = true });
        put.EnsureSuccessStatusCode();

        var get = await client.GetAsync($"/api/v1/admin/companies/{companyId}/config/matricula");
        get.EnsureSuccessStatusCode();
        var body = await get.Content.ReadFromJsonAsync<MatriculaResponse>();
        Assert.True(body!.AllowNewVehicleFiling);
        Assert.True(body.AllowMiscProcedures);
    }

    [Fact]
    public async Task Traffic_authorities_can_be_toggled()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var companyId = await CreateCompanyAsync();
        var client = await _factory.LoginAsSuperAdminAsync();

        var list = await client.GetAsync(
            $"/api/v1/admin/companies/{companyId}/traffic-authorities?page=1&pageSize=5");
        list.EnsureSuccessStatusCode();
        var authorities = await list.Content.ReadFromJsonAsync<TrafficListResponse>();
        var code = authorities!.Items[0].AuthorityCode;

        var patch = await client.PatchAsJsonAsync(
            $"/api/v1/admin/companies/{companyId}/traffic-authorities",
            new { updates = new[] { new { authority_code = code, is_enabled = true } } });
        patch.EnsureSuccessStatusCode();

        var verify = await client.GetAsync(
            $"/api/v1/admin/companies/{companyId}/traffic-authorities?page=1&pageSize=5");
        var updated = await verify.Content.ReadFromJsonAsync<TrafficListResponse>();
        Assert.Contains(updated!.Items, i => i.AuthorityCode == code && i.IsEnabled);
    }

    private async Task<Guid> CreateCompanyAsync()
    {
        var client = await _factory.LoginAsSuperAdminAsync();
        var slug = $"cfg-{Guid.NewGuid():N}"[..16];
        var response = await client.PostAsJsonAsync("/api/v1/admin/companies", new
        {
            mode = "create",
            nit = $"900{Guid.NewGuid():N}"[..12],
            legal_name = "Config Test SAS",
            slug,
            status = "Active"
        });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        return created!.Id;
    }

    private sealed record CreatedResponse(Guid Id, Guid TenantId);
    private sealed record MatriculaResponse(bool AllowNewVehicleFiling, bool AllowMiscProcedures);
    private sealed record TrafficListResponse(List<TrafficItem> Items, int TotalCount, int Page, int PageSize);
    private sealed record TrafficItem(string AuthorityCode, string Name, string? Region, bool IsEnabled);
}
