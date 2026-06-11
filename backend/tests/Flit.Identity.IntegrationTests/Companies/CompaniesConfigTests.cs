using System.Net.Http.Json;

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
}
