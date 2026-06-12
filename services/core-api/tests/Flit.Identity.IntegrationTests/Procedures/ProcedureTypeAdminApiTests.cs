using System.Net;
using System.Net.Http.Json;
using Flit.Procedures.Shared.Domain;

namespace Flit.Identity.IntegrationTests.Procedures;

public class ProcedureTypeAdminApiTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;
    private static HttpClient? _superAdminClient;
    private static readonly SemaphoreSlim LoginGate = new(1, 1);

    public ProcedureTypeAdminApiTests(IdentityWebApplicationFactory factory) => _factory = factory;

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

    [Fact]
    public async Task Create_get_update_and_index_procedure_type()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await SuperAdminAsync();
        var uniqueName = $"Trámite API {Guid.NewGuid():N}"[..24];

        var create = await client.PostAsJsonAsync("/api/v1/admin/procedure-types", new
        {
            name = uniqueName,
            vehicleQueryMode = "Plate",
            actors = new[] { new { roleLabel = "Vendedor" }, new { roleLabel = "Comprador" } },
            documents = new[]
            {
                new { label = "Escritura", kind = "Static" },
                new { label = "Contrato", kind = "Dynamic" },
            },
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CreatedResponse>();
        Assert.NotNull(created);

        var detail = await client.GetAsync($"/api/v1/admin/procedure-types/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var detailBody = await detail.Content.ReadFromJsonAsync<DetailResponse>();
        Assert.NotNull(detailBody);
        Assert.Equal(uniqueName, detailBody!.Name);
        Assert.Equal(2, detailBody.Actors.Count);
        Assert.Equal(2, detailBody.Documents.Count);

        var index = await client.GetAsync("/api/v1/admin/procedure-types/index?name=" + Uri.EscapeDataString(uniqueName));
        Assert.Equal(HttpStatusCode.OK, index.StatusCode);
        var indexBody = await index.Content.ReadFromJsonAsync<IndexResponse>();
        Assert.NotNull(indexBody);
        Assert.Contains(indexBody!.Items, i => i.Id == created.Id);

        var updatedName = $"{uniqueName} Editado";
        var update = await client.PutAsJsonAsync($"/api/v1/admin/procedure-types/{created.Id}", new
        {
            name = updatedName,
            vehicleQueryMode = "Vin",
            actors = new[] { new { roleLabel = "Propietario" } },
            documents = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<DetailResponse>();
        Assert.NotNull(updated);
        Assert.Equal(VehicleQueryMode.Vin, updated!.VehicleQueryMode);
        Assert.Single(updated.Actors);
        Assert.Empty(updated.Documents);
    }

    [Fact]
    public async Task Create_rejects_duplicate_name_and_missing_actors()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await SuperAdminAsync();
        var name = $"Duplicado {Guid.NewGuid():N}"[..20];

        var first = await client.PostAsJsonAsync("/api/v1/admin/procedure-types", new
        {
            name,
            vehicleQueryMode = "Plate",
            actors = new[] { new { roleLabel = "Actor" } },
            documents = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await client.PostAsJsonAsync("/api/v1/admin/procedure-types", new
        {
            name,
            vehicleQueryMode = "Plate",
            actors = new[] { new { roleLabel = "Actor" } },
            documents = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var noActors = await client.PostAsJsonAsync("/api/v1/admin/procedure-types", new
        {
            name = $"Sin actores {Guid.NewGuid():N}",
            vehicleQueryMode = "Plate",
            actors = Array.Empty<object>(),
            documents = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.BadRequest, noActors.StatusCode);
    }

    [Fact]
    public async Task Patch_status_deactivates_without_delete()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await SuperAdminAsync();
        var name = $"Estado {Guid.NewGuid():N}"[..18];

        var create = await client.PostAsJsonAsync("/api/v1/admin/procedure-types", new
        {
            name,
            vehicleQueryMode = "Plate",
            actors = new[] { new { roleLabel = "Interviniente" } },
            documents = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CreatedResponse>();
        Assert.NotNull(created);

        var deactivate = await client.PatchAsJsonAsync(
            $"/api/v1/admin/procedure-types/{created!.Id}/status",
            new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var detail = await deactivate.Content.ReadFromJsonAsync<DetailResponse>();
        Assert.NotNull(detail);
        Assert.False(detail!.IsActive);

        var stillThere = await client.GetAsync($"/api/v1/admin/procedure-types/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
    }

    [Fact]
    public async Task Tenant_admin_cannot_access_procedure_types_api()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await _factory.LoginAsTenantAdminAsync();
        var response = await client.GetAsync("/api/v1/admin/procedure-types/index");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record CreatedResponse(Guid Id, string Code);

    private sealed record IndexResponse(IReadOnlyList<IndexItem> Items, int TotalCount, int Page, int PageSize);

    private sealed record IndexItem(Guid Id, string Name, string Code, bool IsActive);

    private sealed record DetailResponse(
        Guid Id,
        string Name,
        string Code,
        VehicleQueryMode VehicleQueryMode,
        bool IsActive,
        IReadOnlyList<ActorItem> Actors,
        IReadOnlyList<DocumentItem> Documents);

    private sealed record ActorItem(string RoleLabel, int SortOrder);

    private sealed record DocumentItem(string Label, DocumentKind Kind, int SortOrder);
}
