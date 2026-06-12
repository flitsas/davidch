using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Flit.Procedures.Runtime.Create;
using Flit.Procedures.Runtime.Documents;
using Flit.Procedures.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Tramites;

public class TramitesDocumentUploadTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;
    private static HttpClient? _superAdminClient;
    private static readonly SemaphoreSlim LoginGate = new(1, 1);

    public TramitesDocumentUploadTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Upload_valid_pdf_succeeds()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var admin = await SuperAdminAsync();
        var typeId = await CreateProcedureTypeAsync(admin);
        var instanceId = await CreateTramiteAsync(typeId);

        var client = await _factory.LoginAsTenantAdminAsync();
        using var content = BuildMultipartPdf("%PDF-1.4 test content");
        var response = await client.PostAsync(
            $"/api/v1/tramites/{instanceId}/documents?label=Escritura",
            content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var uploaded = await response.Content.ReadFromJsonAsync<DocumentUploadedResponse>();
        Assert.NotNull(uploaded);
        Assert.Equal("Escritura", uploaded.Label);
        Assert.True(uploaded.FileSizeBytes > 0);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProceduresDbContext>();
        var row = db.ProcedureInstanceDocuments.Single(d => d.ProcedureInstanceId == instanceId);
        Assert.Equal("Escritura", row.Label);
        Assert.False(string.IsNullOrWhiteSpace(row.StoragePath));
    }

    [Fact]
    public async Task Upload_non_pdf_renamed_as_pdf_returns_400()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var admin = await SuperAdminAsync();
        var typeId = await CreateProcedureTypeAsync(admin);
        var instanceId = await CreateTramiteAsync(typeId);

        var client = await _factory.LoginAsTenantAdminAsync();
        using var content = BuildMultipartPdf("plain text, not a pdf");
        var response = await client.PostAsync(
            $"/api/v1/tramites/{instanceId}/documents?label=Escritura",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> CreateTramiteAsync(Guid typeId)
    {
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
            documents = new[]
            {
                new { label = "Escritura", fileName = "escritura.pdf", fileSizeBytes = 1024L },
            },
        });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<TramiteCreatedResponse>();
        return created!.Id;
    }

    private static MultipartFormDataContent BuildMultipartPdf(string payload)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "document.pdf");
        return form;
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

    private static async Task<Guid> CreateProcedureTypeAsync(HttpClient admin)
    {
        var uniqueName = $"Tramite {Guid.NewGuid():N}"[..20];
        var create = await admin.PostAsJsonAsync("/api/v1/admin/procedure-types", new
        {
            name = uniqueName,
            vehicleQueryMode = "Plate",
            actors = new[] { new { roleLabel = "Vendedor" }, new { roleLabel = "Comprador" } },
            documents = new[] { new { label = "Escritura", kind = "Static" } },
        });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatedResponse>();
        return created!.Id;
    }

    private sealed record CreatedResponse(Guid Id);
}
