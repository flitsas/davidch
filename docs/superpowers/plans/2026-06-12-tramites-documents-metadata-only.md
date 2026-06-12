# Trámites Document Metadata-Only Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix HTTP 413 on trámite confirm by registering PDF metadata in `POST /api/v1/tramites` instead of multipart uploads; wizard keeps file picker for client validation only.

**Architecture:** Extend `CreateTramiteRequest` with a `documents[]` array. `TramitesCreateHandler` validates labels against static definition documents, inserts `procedure_instance_documents` rows with `storagePath = ""` in the same transaction as the instance. Frontend sends metadata only on confirm; multipart upload endpoint stays for future use.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, EF Core + Npgsql, xUnit + Testcontainers, Next.js 16, React 19, TypeScript, Playwright

**Spec:** `docs/superpowers/specs/2026-06-12-tramites-documents-metadata-only-design.md`

**Suggested branch:** `feature/tramites-documents-metadata-only`

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `services/core-api/src/Flit.Procedures.Runtime/Create/TramitesCreateModels.cs` | Modify | Add `CreateTramiteDocumentRequest`; extend `CreateTramiteRequest` |
| `services/core-api/src/Flit.Procedures.Runtime/Create/TramitesCreateHandler.cs` | Modify | Document validation + insert rows |
| `services/core-api/src/Flit.Identity.Api/appsettings.Development.json` | Modify | Optional `Procedures:MaxDocumentSizeBytes` |
| `services/core-api/tests/.../Tramites/TramitesCreateDocumentsTests.cs` | Create | New integration tests for document metadata |
| `services/core-api/tests/.../Tramites/TramitesCreateTests.cs` | Modify | Add `documents` to happy-path payloads |
| `services/core-api/tests/.../Tramites/Dashboard/TramitesDashboardTestHelper.cs` | Modify | Add `documents` to seed helper |
| `services/core-api/tests/.../Tramites/TramitesDocumentUploadTests.cs` | Modify | Add `documents` to inline create helper |
| `frontend/lib/tramites/client-api.ts` | Modify | Send `documents` in `createTramite` |
| `frontend/components/tramites/ProcedureInstanceWizard.tsx` | Modify | Size validation; remove upload loop |
| `frontend/e2e/tramites/tramites-create.spec.ts` | Verify | Should pass unchanged (single POST) |

---

## Task 1: Extend create DTOs

**Files:**
- Modify: `services/core-api/src/Flit.Procedures.Runtime/Create/TramitesCreateModels.cs`

- [ ] **Step 1: Add document request record and extend create request**

Replace file contents with:

```csharp
using System.Text.Json.Serialization;
using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Runtime.Create;

public sealed record CreateTramiteRequest(
    Guid ProcedureTypeId,
    string OtDivipolCode,
    string VehicleQueryValue,
    IReadOnlyList<CreateTramiteActorRequest> Actors,
    IReadOnlyList<CreateTramiteDocumentRequest>? Documents);

public sealed record CreateTramiteDocumentRequest(
    string Label,
    string FileName,
    long FileSizeBytes);

public sealed record CreateTramiteActorRequest(
    string RoleLabel,
    int SortOrder,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] PersonKind PersonKind,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] DocumentIdType DocumentType,
    string DocumentNumber,
    CreateLegalRepresentativeRequest? LegalRepresentative);

public sealed record CreateLegalRepresentativeRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] DocumentIdType DocumentType,
    string DocumentNumber);

public sealed record TramiteCreatedResponse(Guid Id, string Status);
```

- [ ] **Step 2: Build to verify compile**

Run: `dotnet build services/core-api/src/Flit.Identity.Api/Flit.Identity.Api.csproj`

Expected: succeeds (handler still accepts old call sites with `Documents` defaulting to null via JSON omission — C# may need call site updates in tests only after handler enforces docs)

- [ ] **Step 3: Commit**

```bash
git add services/core-api/src/Flit.Procedures.Runtime/Create/TramitesCreateModels.cs
git commit -m "feat(tramites): extend create request with document metadata DTO"
```

---

## Task 2: Document metadata integration tests (TDD)

**Files:**
- Create: `services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/TramitesCreateDocumentsTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
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
        if (!_factory.IsDockerAvailable) return;

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
        if (!_factory.IsDockerAvailable) return;

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
        if (!_factory.IsDockerAvailable) return;

        await _factory.EnsureTenantSeededAsync();
        var admin = await SuperAdminAsync();
        var typeId = await CreateProcedureTypeAsync(admin, documentLabel: "Escritura");

        var client = await _factory.LoginAsTenantAdminAsync();
        var payload = BuildCreatePayload(typeId, "Escritura");
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
        if (!_factory.IsDockerAvailable) return;

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
        if (_superAdminClient is not null) return _superAdminClient;
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test services/core-api/tests/Flit.Identity.IntegrationTests/Flit.Identity.IntegrationTests.csproj --filter "FullyQualifiedName~TramitesCreateDocumentsTests" --no-build`  
(if build needed first: `dotnet build` then re-run)

Expected: FAIL — happy path has no document rows or returns 400; validation tests may not fail yet.

- [ ] **Step 3: Commit failing tests**

```bash
git add services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/TramitesCreateDocumentsTests.cs
git commit -m "test(tramites): add document metadata create integration tests"
```

---

## Task 3: Implement document validation and persistence in create handler

**Files:**
- Modify: `services/core-api/src/Flit.Procedures.Runtime/Create/TramitesCreateHandler.cs`

- [ ] **Step 1: Inject `IConfiguration` and add validation helper**

Add to constructor parameter list:

```csharp
public sealed class TramitesCreateHandler(
    ProceduresDbContext db,
    IProcedureDefinitionService definitions,
    TrafficAuthorityPicker trafficAuthorityPicker,
    IConfiguration configuration)
```

After actor validation block (before `var now = DateTimeOffset.UtcNow`), add:

```csharp
        var maxDocumentSizeBytes = configuration.GetValue("Procedures:MaxDocumentSizeBytes", 10 * 1024 * 1024);
        var documentValidation = ValidateDocuments(request.Documents, definition, maxDocumentSizeBytes);
        if (documentValidation is not null)
        {
            return documentValidation;
        }
```

After the actor `foreach` loop and before `db.ProcedureInstances.Add(instance)`, add document entities:

```csharp
        var staticDocuments = definition.Documents
            .Where(d => d.Kind == DocumentKind.Static)
            .ToList();
        var providedDocuments = request.Documents ?? Array.Empty<CreateTramiteDocumentRequest>();

        foreach (var docDef in staticDocuments)
        {
            var docRequest = providedDocuments.Single(d =>
                string.Equals(d.Label.Trim(), docDef.Label, StringComparison.Ordinal));

            instance.Documents.Add(new ProcedureInstanceDocument
            {
                Id = Guid.NewGuid(),
                ProcedureInstanceId = instance.Id,
                Label = docDef.Label,
                Kind = DocumentKind.Static,
                FileName = Path.GetFileName(docRequest.FileName.Trim()),
                FileSizeBytes = docRequest.FileSizeBytes,
                StoragePath = "",
                UploadedAt = now,
            });
        }
```

Note: move `var now = DateTimeOffset.UtcNow` **before** document loop so `UploadedAt` is available (relocate existing `now` declaration up, remove duplicate).

Add private method at bottom of class:

```csharp
    private static IResult? ValidateDocuments(
        IReadOnlyList<CreateTramiteDocumentRequest>? documents,
        ProcedureDefinitionDto definition,
        long maxDocumentSizeBytes)
    {
        var requiredLabels = definition.Documents
            .Where(d => d.Kind == DocumentKind.Static)
            .Select(d => d.Label)
            .ToList();

        var provided = documents ?? Array.Empty<CreateTramiteDocumentRequest>();

        if (requiredLabels.Count == 0)
        {
            return provided.Count > 0
                ? ValidationError("Document count does not match procedure definition.")
                : null;
        }

        if (provided.Count != requiredLabels.Count)
        {
            return ValidationError("Document count does not match procedure definition.");
        }

        foreach (var required in requiredLabels)
        {
            if (!provided.Any(d => string.Equals(d.Label.Trim(), required, StringComparison.Ordinal)))
            {
                return ValidationError($"Falta el documento requerido: {required}.");
            }
        }

        foreach (var doc in provided)
        {
            if (!requiredLabels.Any(label =>
                    string.Equals(label, doc.Label.Trim(), StringComparison.Ordinal)))
            {
                return ValidationError("Document label is not a static document in this procedure type.");
            }

            if (string.IsNullOrWhiteSpace(doc.FileName)
                || !doc.FileName.Trim().EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return ValidationError("fileName must be a PDF file name.");
            }

            if (doc.FileSizeBytes <= 0 || doc.FileSizeBytes > maxDocumentSizeBytes)
            {
                return ValidationError("fileSizeBytes exceeds maximum allowed (10 MB).");
            }
        }

        return null;
    }
```

Add usings if missing: `Microsoft.Extensions.Configuration`, `Flit.Procedures.Infrastructure.Persistence.Entities`.

- [ ] **Step 2: Run document tests**

Run: `dotnet test services/core-api/tests/Flit.Identity.IntegrationTests/Flit.Identity.IntegrationTests.csproj --filter "FullyQualifiedName~TramitesCreateDocumentsTests"`

Expected: PASS (all 4 tests)

- [ ] **Step 3: Commit**

```bash
git add services/core-api/src/Flit.Procedures.Runtime/Create/TramitesCreateHandler.cs
git commit -m "feat(tramites): persist document metadata on create"
```

---

## Task 4: Fix existing integration tests that create trámites

**Files:**
- Modify: `services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/TramitesCreateTests.cs`
- Modify: `services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/Dashboard/TramitesDashboardTestHelper.cs`
- Modify: `services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/TramitesDocumentUploadTests.cs`

- [ ] **Step 1: Add shared documents payload to each create call**

In each `PostAsJsonAsync("/api/v1/tramites", new { ... })` where the procedure type includes `Escritura` static document, add:

```csharp
            documents = new[]
            {
                new { label = "Escritura", fileName = "escritura.pdf", fileSizeBytes = 1024L },
            },
```

Files/locations:
- `TramitesCreateTests.cs` — both happy-path tests (`Create_tramite_with_natural_actors_succeeds`, `Create_tramite_with_juridical_actor_requires_legal_representative`)
- `TramitesDashboardTestHelper.cs` — `CreateTramiteAsync` private method
- `TramitesDocumentUploadTests.cs` — `CreateTramiteAsync` private method

- [ ] **Step 2: Run all Tramites tests**

Run: `dotnet test services/core-api/tests/Flit.Identity.IntegrationTests/Flit.Identity.IntegrationTests.csproj --filter "FullyQualifiedName~Tramites"`

Expected: all PASS

- [ ] **Step 3: Commit**

```bash
git add services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/
git commit -m "test(tramites): include document metadata in existing create payloads"
```

---

## Task 5: Optional config default

**Files:**
- Modify: `services/core-api/src/Flit.Identity.Api/appsettings.Development.json`

- [ ] **Step 1: Document max size in dev settings**

Add top-level key (merge into existing JSON):

```json
"Procedures": {
  "MaxDocumentSizeBytes": 10485760
}
```

- [ ] **Step 2: Commit**

```bash
git add services/core-api/src/Flit.Identity.Api/appsettings.Development.json
git commit -m "chore(tramites): document MaxDocumentSizeBytes dev default"
```

---

## Task 6: Frontend — send metadata, remove multipart loop

**Files:**
- Modify: `frontend/lib/tramites/client-api.ts`
- Modify: `frontend/components/tramites/ProcedureInstanceWizard.tsx`

- [ ] **Step 1: Extend `createTramite` payload**

In `frontend/lib/tramites/client-api.ts`, update `createTramite`:

```typescript
const MAX_DOCUMENT_BYTES = 10 * 1024 * 1024;

export async function createTramite(payload: {
  procedureTypeId: string;
  otDivipolCode: string;
  vehicleQueryValue: string;
  actors: ActorFormState[];
  documents: DocumentFileState[];
}) {
  const res = await fetch("/api/v1/tramites", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      procedureTypeId: payload.procedureTypeId,
      otDivipolCode: payload.otDivipolCode,
      vehicleQueryValue: payload.vehicleQueryValue,
      actors: payload.actors.map((actor) => ({
        roleLabel: actor.roleLabel,
        sortOrder: actor.sortOrder,
        personKind: actor.personKind,
        documentType: actor.documentType,
        documentNumber: actor.documentNumber,
        legalRepresentative: actor.legalRepresentative ?? null,
      })),
      documents: payload.documents
        .filter((doc) => doc.file)
        .map((doc) => ({
          label: doc.label,
          fileName: doc.file!.name,
          fileSizeBytes: doc.file!.size,
        })),
    }),
  });
  return parseJson<{ id: string; status: string }>(res);
}

export { MAX_DOCUMENT_BYTES };
```

Remove `uploadTramiteDocument` import usage from wizard (function may remain in file with a one-line comment `// Reserved for future binary upload`).

- [ ] **Step 2: Update wizard validation and submit**

In `ProcedureInstanceWizard.tsx`:

Add import: `MAX_DOCUMENT_BYTES` from `@/lib/tramites/client-api`.

In `validateStep` for step 5, after `isPdfFile` check:

```typescript
        if (doc.file.size > MAX_DOCUMENT_BYTES) {
          return `El archivo ${doc.label} supera el máximo de 10 MB.`;
        }
```

Remove `uploadTramiteDocument` from imports.

Replace `submit()` body try block:

```typescript
      await createTramite({
        procedureTypeId: selectedTypeId,
        otDivipolCode,
        vehicleQueryValue: vehicleQueryValue.trim(),
        actors,
        documents,
      });

      onSuccess();
```

- [ ] **Step 3: Lint frontend**

Run: `cd frontend && pnpm lint`

Expected: no errors

- [ ] **Step 4: Commit**

```bash
git add frontend/lib/tramites/client-api.ts frontend/components/tramites/ProcedureInstanceWizard.tsx
git commit -m "feat(tramites): send document metadata on create, drop multipart upload loop"
```

---

## Task 7: Verification

- [ ] **Step 1: Run backend Tramites filter**

Run: `dotnet test services/core-api/tests/Flit.Identity.IntegrationTests/Flit.Identity.IntegrationTests.csproj --filter "FullyQualifiedName~Tramites"`

Expected: all PASS

- [ ] **Step 2: Run E2E (if stack available)**

Run: `cd frontend && pnpm exec playwright test e2e/tramites/tramites-create.spec.ts`

Expected: PASS — single `POST /api/v1/tramites` with JSON body including `documents`

- [ ] **Step 3: Manual smoke on dev**

1. Open wizard, complete all steps with a real PDF on step 5.
2. Confirm trámite — Network tab should show **one** `POST /api/v1/tramites` (~few KB), **no** `POST .../documents`.
3. Response 201; trámite appears in grid.

---

## Spec coverage checklist

| Spec requirement | Task |
|------------------|------|
| JSON metadata in create request | Task 1, 3, 6 |
| Atomic transaction | Task 3 |
| `storagePath = ""` | Task 3, verified Task 2 |
| Max 10 MB validation server + client | Task 3, 5, 6 |
| Label exact match / missing / unknown errors | Task 2, 3 |
| Wizard keeps PDF picker | Task 6 |
| Multipart endpoint unchanged | No task (no code change) |
| Update existing tests | Task 4 |
| E2E single POST | Task 7 |
