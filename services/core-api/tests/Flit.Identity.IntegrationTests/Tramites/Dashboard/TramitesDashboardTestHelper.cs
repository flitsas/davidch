using System.Net.Http.Json;

namespace Flit.Identity.IntegrationTests.Tramites.Dashboard;

internal static class TramitesDashboardTestHelper
{
    internal sealed record SeedResult(string MatriculaPlate, string TraspasoPlate, string OtrosPlate);

    internal static async Task<SeedResult> SeedThreeCategoryInstancesAsync(
        HttpClient superAdmin,
        HttpClient tenantAdmin)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var matriculaPlate = $"MAT{suffix}";
        var traspasoPlate = $"TRP{suffix}";
        var otrosPlate = $"OTR{suffix}";

        var matriculaTypeId = await CreateProcedureTypeAsync(superAdmin, $"Matricula Inicial {suffix}");
        var traspasoTypeId = await GetOrCreateTraspasoTypeAsync(superAdmin);
        var otrosTypeId = await CreateProcedureTypeAsync(superAdmin, $"Radicado Cuenta {suffix}");

        await CreateTramiteAsync(tenantAdmin, matriculaTypeId, matriculaPlate);
        await CreateTramiteAsync(tenantAdmin, traspasoTypeId, traspasoPlate);
        await CreateTramiteAsync(tenantAdmin, otrosTypeId, otrosPlate);

        return new SeedResult(matriculaPlate, traspasoPlate, otrosPlate);
    }

    private static async Task<Guid> GetOrCreateTraspasoTypeAsync(HttpClient admin)
    {
        var list = await admin.GetAsync("/api/v1/admin/procedure-types/index?page=1&pageSize=100");
        list.EnsureSuccessStatusCode();
        var index = await list.Content.ReadFromJsonAsync<ProcedureTypeIndexResponse>();
        var existing = index?.Items.FirstOrDefault(i =>
            string.Equals(i.Code, "TRASPASO", StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing.Id;
        }

        return await CreateProcedureTypeAsync(admin, "Traspaso");
    }

    private static async Task<Guid> CreateProcedureTypeAsync(HttpClient admin, string name)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/admin/procedure-types", new
        {
            name,
            vehicleQueryMode = "Plate",
            actors = new[] { new { roleLabel = "Comprador" } },
            documents = new[] { new { label = "Escritura", kind = "Static" } },
        });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        return created!.Id;
    }

    private static async Task CreateTramiteAsync(HttpClient client, Guid procedureTypeId, string plate)
    {
        var response = await client.PostAsJsonAsync("/api/v1/tramites", new
        {
            procedureTypeId,
            otDivipolCode = "11001000",
            vehicleQueryValue = plate,
            actors = new[]
            {
                new
                {
                    roleLabel = "Comprador",
                    sortOrder = 1,
                    personKind = "Natural",
                    documentType = "Cc",
                    documentNumber = "1234567890",
                    legalRepresentative = (object?)null,
                },
            },
        });
        response.EnsureSuccessStatusCode();
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record ProcedureTypeIndexResponse(IReadOnlyList<ProcedureTypeIndexItem> Items);

    private sealed record ProcedureTypeIndexItem(Guid Id, string Code);
}
