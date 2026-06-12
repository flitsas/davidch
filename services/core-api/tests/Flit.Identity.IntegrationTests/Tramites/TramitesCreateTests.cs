using System.Net;
using System.Net.Http.Json;
using Flit.Procedures.Runtime.Create;
using Flit.Procedures.Shared.Domain;

namespace Flit.Identity.IntegrationTests.Tramites;

public class TramitesCreateTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;
    private static HttpClient? _superAdminClient;
    private static readonly SemaphoreSlim LoginGate = new(1, 1);

    public TramitesCreateTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_tramite_with_natural_actors_succeeds()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var admin = await SuperAdminAsync();
        var typeId = await CreateProcedureTypeAsync(admin);

        var client = await _factory.LoginAsTenantAdminAsync();
        var response = await client.PostAsJsonAsync("/api/v1/tramites", new
        {
            procedureTypeId = typeId,
            otDivipolCode = "11001000",
            vehicleQueryValue = "ABC123",
            actors = new[]
            {
                new
                {
                    roleLabel = "Vendedor",
                    sortOrder = 1,
                    personKind = "Natural",
                    documentType = "Cc",
                    documentNumber = "1234567890",
                    legalRepresentative = (object?)null,
                },
                new
                {
                    roleLabel = "Comprador",
                    sortOrder = 2,
                    personKind = "Natural",
                    documentType = "Cc",
                    documentNumber = "0987654321",
                    legalRepresentative = (object?)null,
                },
            },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TramiteCreatedResponse>();
        Assert.NotNull(created);
        Assert.Equal("PendienteEnvio", created.Status);
    }

    [Fact]
    public async Task Create_tramite_with_juridical_actor_requires_legal_representative()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var admin = await SuperAdminAsync();
        var typeId = await CreateProcedureTypeAsync(admin, singleActor: true);

        var client = await _factory.LoginAsTenantAdminAsync();
        var response = await client.PostAsJsonAsync("/api/v1/tramites", new
        {
            procedureTypeId = typeId,
            otDivipolCode = "11001000",
            vehicleQueryValue = "XYZ999",
            actors = new[]
            {
                new
                {
                    roleLabel = "Comprador",
                    sortOrder = 1,
                    personKind = "Juridica",
                    documentType = "Nit",
                    documentNumber = "900123456",
                    legalRepresentative = new
                    {
                        documentType = "Cc",
                        documentNumber = "1111111111",
                    },
                },
            },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Lookup_stubs_return_ok()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsTenantAdminAsync();

        var rues = await client.GetAsync("/api/v1/tramites/lookups/rues?nit=900123456");
        Assert.Equal(HttpStatusCode.OK, rues.StatusCode);

        var simit = await client.GetAsync("/api/v1/tramites/lookups/simit?documentType=Cc&documentNumber=123");
        Assert.Equal(HttpStatusCode.OK, simit.StatusCode);
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

    private static async Task<Guid> CreateProcedureTypeAsync(HttpClient admin, bool singleActor = false)
    {
        var uniqueName = $"Tramite {Guid.NewGuid():N}"[..20];
        var actors = singleActor
            ? new[] { new { roleLabel = "Comprador" } }
            : new[] { new { roleLabel = "Vendedor" }, new { roleLabel = "Comprador" } };

        var create = await admin.PostAsJsonAsync("/api/v1/admin/procedure-types", new
        {
            name = uniqueName,
            vehicleQueryMode = "Plate",
            actors,
            documents = new[] { new { label = "Escritura", kind = "Static" } },
        });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatedResponse>();
        return created!.Id;
    }

    private sealed record CreatedResponse(Guid Id);
}
