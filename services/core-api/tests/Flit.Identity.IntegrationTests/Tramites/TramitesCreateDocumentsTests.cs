using System.Net;
using System.Net.Http.Json;
using Flit.Procedures.Infrastructure.Persistence;
using Flit.Procedures.Runtime.Create;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Tramites;

public class TramitesCreateDocumentsTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;
    private static HttpClient? _superAdminClient;
    private static readonly SemaphoreSlim LoginGate = new(1, 1);

    public TramitesCreateDocumentsTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_with_document_metadata_persists_rows_with_empty_storage_path()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var admin = await SuperAdminAsync();
        var typeId = await CreateProcedureTypeAsync(admin, documentLabel: "Escritura");

        var client = await _factory.LoginAsTenantAdminAsync();
        var response = await client.PostAsJsonAsync("/api/v1/tramites", BuildCreatePayload(typeId, "Escritura"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TramiteCreatedResponse>();
        Assert.NotNull(created);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProceduresDbContext>();
        var row = db.ProcedureInstanceDocuments.Single(d => d.ProcedureInstanceId == created.Id);
        Assert.Equal("Escritura", row.Label);
        Assert.Equal("escritura.pdf", row.FileName);
        Assert.Equal(1024, row.FileSizeBytes);
        Assert.Equal("", row.StoragePath);
    }

    [Fact]
    public async Task Create_missing_required_document_returns_400()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var admin = await SuperAdminAsync();
        var typeId = await CreateProcedureTypeAsync(admin, documentLabel: "IMPRONTA");

        var client = await _factory.LoginAsTenantAdminAsync();
        var response = await client.PostAsJsonAsync("/api/v1/tramites", new
        {
            procedureTypeId = typeId,
            otDivipolCode = "11001000",
            vehicleQueryValue = "ABC123",
            actors = BuildActors(),
            documents = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_unknown_document_label_returns_400()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var admin = await SuperAdminAsync();
        var typeId = await CreateProcedureTypeAsync(admin, documentLabel: "Escritura");

        var client = await _factory.LoginAsTenantAdminAsync();
        var withExtra = new Dictionary<string, object?>
        {
            ["procedureTypeId"] = typeId,
            ["otDivipolCode"] = "11001000",
            ["vehicleQueryValue"] = "ABC123",
            ["actors"] = BuildActors(),
            ["documents"] = new object[]
            {
                new { label = "Escritura", fileName = "escritura.pdf", fileSizeBytes = 1024L },
                new { label = "Extra", fileName = "extra.pdf", fileSizeBytes = 512L },
            },
        };
        var response = await client.PostAsJsonAsync("/api/v1/tramites", withExtra);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_oversized_fileSizeBytes_returns_400()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var admin = await SuperAdminAsync();
        var typeId = await CreateProcedureTypeAsync(admin, documentLabel: "Escritura");

        var client = await _factory.LoginAsTenantAdminAsync();
        var response = await client.PostAsJsonAsync("/api/v1/tramites", new
        {
            procedureTypeId = typeId,
            otDivipolCode = "11001000",
            vehicleQueryValue = "ABC123",
            actors = BuildActors(),
            documents = new[]
            {
                new { label = "Escritura", fileName = "escritura.pdf", fileSizeBytes = 11_000_000L },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static object BuildCreatePayload(Guid typeId, string documentLabel) => new
    {
        procedureTypeId = typeId,
        otDivipolCode = "11001000",
        vehicleQueryValue = "ABC123",
        actors = BuildActors(),
        documents = new[]
        {
            new { label = documentLabel, fileName = "escritura.pdf", fileSizeBytes = 1024L },
        },
    };

    private static object[] BuildActors() =>
    [
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
    ];

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

    private static async Task<Guid> CreateProcedureTypeAsync(HttpClient admin, string documentLabel)
    {
        var uniqueName = $"Tramite {Guid.NewGuid():N}"[..20];
        var create = await admin.PostAsJsonAsync("/api/v1/admin/procedure-types", new
        {
            name = uniqueName,
            vehicleQueryMode = "Plate",
            actors = new[] { new { roleLabel = "Vendedor" }, new { roleLabel = "Comprador" } },
            documents = new[] { new { label = documentLabel, kind = "Static" } },
        });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatedResponse>();
        return created!.Id;
    }

    private sealed record CreatedResponse(Guid Id);
}
