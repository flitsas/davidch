# Trámites Runtime MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver MVP A for Feature #9733 — company operators create procedure instances from parametrized types, select OT, capture vehicle/actors/documents, persist as `PendienteEnvio`.

**Architecture:** New `Flit.Procedures.Runtime` in the core-api monolith. Extends `ProceduresDbContext` with instance tables. APIs at `/api/v1/tramites/*` with `tramites:read`/`tramites:create` RBAC. Frontend at `/tramites` with modal 6-step wizard. PDFs on local volume.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, EF Core 10 + Npgsql, PostgreSQL 16, Next.js 16, React 19, TypeScript, Tailwind v4, pnpm 9, xUnit + Testcontainers, Playwright

**Spec:** `docs/superpowers/specs/2026-06-12-tramites-runtime-mvp-design.md`

**ADO Feature:** [#9733](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9733) (Active)

| Story | ADO | Title | Branch |
|-------|-----|-------|--------|
| HU-1 | #10071 | Schema instancias + migración | `feature/HU10071-DCHICA-schema-procedure-instances` |
| HU-2 | #10072 | API runtime (index, create, OT, stubs) | `feature/HU10072-DCHICA-api-tramites-runtime` |
| HU-3 | #10073 | Upload PDF + storage local | `feature/HU10073-DCHICA-api-tramites-documents` |
| HU-4 | #10074 | FE grilla + nav + permisos | `feature/HU10074-DCHICA-fe-tramites-index` |
| HU-5 | #10075 | FE modal wizard 6 pasos | `feature/HU10075-DCHICA-fe-tramites-wizard` |

**Dependency order:** HU-1 → HU-2 → HU-3 → HU-4 → HU-5. Stop for review between HUs.

---

## File Map

### Backend (`services/core-api/`)

| File | Responsibility |
|------|----------------|
| `Flit.Identity.sln` | Add Procedures.Runtime project |
| `src/Flit.Procedures.Shared/Domain/ProcedureStatus.cs` | `PendienteEnvio` |
| `src/Flit.Procedures.Shared/Domain/PersonKind.cs` | `Natural`, `Juridica` |
| `src/Flit.Procedures.Shared/Domain/DocumentIdType.cs` | `Cc`, `Ce`, `Passport`, `Nit` |
| `src/Flit.Procedures.Infrastructure/Persistence/Entities/ProcedureInstance.cs` | Instance root |
| `src/Flit.Procedures.Infrastructure/Persistence/Entities/ProcedureInstanceActor.cs` | Actors + legal rep |
| `src/Flit.Procedures.Infrastructure/Persistence/Entities/ProcedureInstanceDocument.cs` | PDF metadata |
| `src/Flit.Procedures.Infrastructure/Persistence/ProceduresDbContext.cs` | Add DbSets + config |
| `src/Flit.Procedures.Infrastructure/Migrations/*` | EF migration |
| `src/Flit.Procedures.Runtime/Flit.Procedures.Runtime.csproj` | Runtime module |
| `src/Flit.Procedures.Runtime/Auth/RequireTramitesPermissionExtensions.cs` | `tramites:read`/`create` filter |
| `src/Flit.Procedures.Runtime/Index/TramitesIndexHandler.cs` | Paginated grid |
| `src/Flit.Procedures.Runtime/Create/TramitesCreateHandler.cs` | POST create + validation |
| `src/Flit.Procedures.Runtime/Ot/TrafficAuthorityPicker.cs` | Matrix ∩ OT profiles |
| `src/Flit.Procedures.Runtime/Lookups/StubExternalLookupService.cs` | RUES/SIMIT/RNMC stubs |
| `src/Flit.Procedures.Runtime/Documents/ProcedureDocumentStorage.cs` | Local PDF I/O |
| `src/Flit.Procedures.Runtime/Documents/TramitesDocumentUploadHandler.cs` | Multipart upload |
| `src/Flit.Procedures.Runtime/Endpoints/TramitesEndpoints.cs` | Route map |
| `src/Flit.Identity.Api/Program.cs` | DI + `MapTramitesEndpoints` |
| `src/Flit.Identity.Infrastructure/Persistence/Seed/DevTenantSeeder.cs` | `tramites:read`, `tramites:create` |

### Frontend (`frontend/`)

| File | Responsibility |
|------|----------------|
| `lib/tramites/types.ts` | DTO types |
| `lib/tramites/api.ts` | API helpers |
| `lib/flit/nav.ts` | Trámites nav item |
| `app/tramites/page.tsx` | Server page + grid |
| `app/tramites/layout.tsx` | Auth guard `tramites:read` |
| `components/tramites/TramitesIndexTable.tsx` | Grid |
| `components/tramites/ProcedureInstanceWizard.tsx` | 6-step modal |
| `components/tramites/wizard/*` | Step subcomponents |
| `e2e/tramites/tramites-index.spec.ts` | E2E grid |
| `e2e/tramites/tramites-create.spec.ts` | E2E wizard |

### Infra

| File | Responsibility |
|------|----------------|
| `docker-compose.yml` | Volume `flit_procedure_docs` + env `Procedures__DocumentStoragePath` |

### Tests

| File | Responsibility |
|------|----------------|
| `Flit.Identity.IntegrationTests/Tramites/TramitesMigrationTests.cs` | Tables exist |
| `Flit.Identity.IntegrationTests/Tramites/TramitesIndexTests.cs` | Index + isolation |
| `Flit.Identity.IntegrationTests/Tramites/TramitesCreateTests.cs` | Create + jurídica |
| `Flit.Identity.IntegrationTests/Tramites/TramitesDocumentUploadTests.cs` | PDF validation |
| `Flit.Identity.IntegrationTests/Tramites/TramitesAuthTests.cs` | 403 cases |

---

## HU-1: Schema instancias (#10071)

**Branch:** `feature/HU10071-DCHICA-schema-procedure-instances`

### Task 1: Domain enums

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Shared/Domain/ProcedureStatus.cs`
- Create: `services/core-api/src/Flit.Procedures.Shared/Domain/PersonKind.cs`
- Create: `services/core-api/src/Flit.Procedures.Shared/Domain/DocumentIdType.cs`

- [ ] **Step 1: Add enums**

`ProcedureStatus.cs`:

```csharp
namespace Flit.Procedures.Shared.Domain;

public enum ProcedureStatus
{
    PendienteEnvio = 0,
}
```

`PersonKind.cs`:

```csharp
namespace Flit.Procedures.Shared.Domain;

public enum PersonKind
{
    Natural = 0,
    Juridica = 1,
}
```

`DocumentIdType.cs`:

```csharp
namespace Flit.Procedures.Shared.Domain;

public enum DocumentIdType
{
    Cc = 0,
    Ce = 1,
    Passport = 2,
    Nit = 3,
}
```

- [ ] **Step 2: Build Shared**

```bash
cd /home/david/davidch/services/core-api
dotnet build src/Flit.Procedures.Shared/Flit.Procedures.Shared.csproj
```

Expected: Build succeeded.

### Task 2: EF entities

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Infrastructure/Persistence/Entities/ProcedureInstance.cs`
- Create: `services/core-api/src/Flit.Procedures.Infrastructure/Persistence/Entities/ProcedureInstanceActor.cs`
- Create: `services/core-api/src/Flit.Procedures.Infrastructure/Persistence/Entities/ProcedureInstanceDocument.cs`
- Modify: `services/core-api/src/Flit.Procedures.Infrastructure/Persistence/ProceduresDbContext.cs`

- [ ] **Step 1: Create entity classes**

`ProcedureInstance.cs` — properties: `Id`, `TenantId`, `ProcedureTypeId`, `ProcedureTypeCode`, `OtTenantId`, `OtDivipolCode`, `VehicleQueryValue`, `Status`, `CreatedBy`, `CreatedAt`, `UpdatedAt`, collections for Actors/Documents.

`ProcedureInstanceActor.cs` — include `ParentActorId` nullable self-FK, `IsLegalRepresentative`, `ExternalDataJson` as `string?`.

`ProcedureInstanceDocument.cs` — `StoragePath`, `FileName`, `FileSizeBytes`, `Label`, `Kind` (reuse `DocumentKind`), `UploadedAt`.

- [ ] **Step 2: Register in DbContext**

Add DbSets and `OnModelCreating` indexes:
- `procedure_instances`: index `(tenant_id, created_at)`
- `procedure_instance_actors`: index `(procedure_instance_id, sort_order)`
- `procedure_instance_documents`: unique `(procedure_instance_id, label)`

- [ ] **Step 3: Add migration**

```bash
cd /home/david/davidch/services/core-api
dotnet ef migrations add AddProcedureInstances \
  --project src/Flit.Procedures.Infrastructure \
  --startup-project src/Flit.Identity.Api \
  --context ProceduresDbContext
```

Expected: Migration file under `Migrations/`.

### Task 3: Migration integration test

**Files:**
- Create: `services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/TramitesMigrationTests.cs`

- [ ] **Step 1: Write test**

```csharp
[Fact]
public async Task Procedures_migration_creates_instance_tables()
{
    if (!_factory.IsDockerAvailable) return;

    await using var scope = _factory.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ProceduresDbContext>();
    var tables = await db.Database.SqlQueryRaw<string>(
        "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'").ToListAsync();

    Assert.Contains("procedure_instances", tables);
    Assert.Contains("procedure_instance_actors", tables);
    Assert.Contains("procedure_instance_documents", tables);
}
```

- [ ] **Step 2: Run test**

```bash
cd /home/david/davidch/services/core-api
dotnet test tests/Flit.Identity.IntegrationTests --filter TramitesMigrationTests -v n
```

Expected: PASS

---

## HU-2: API runtime (#10072)

**Branch:** `feature/HU10072-DCHICA-api-tramites-runtime`

### Task 1: Runtime project scaffold

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Runtime/Flit.Procedures.Runtime.csproj`
- Modify: `services/core-api/Flit.Identity.sln`
- Modify: `services/core-api/src/Flit.Identity.Api/Flit.Identity.Api.csproj`

- [ ] **Step 1: Create project with refs to Shared, Infrastructure, Companies.Infrastructure, OT.Infrastructure, Identity.Rbac**

```bash
cd /home/david/davidch/services/core-api/src
dotnet new classlib -n Flit.Procedures.Runtime -o Flit.Procedures.Runtime -f net10.0
dotnet sln ../Flit.Identity.sln add Flit.Procedures.Runtime/Flit.Procedures.Runtime.csproj
```

Add package refs: `Microsoft.AspNetCore.Http.Abstractions`, project refs as listed.

### Task 2: Auth filter

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Runtime/Auth/RequireTramitesPermissionExtensions.cs`

- [ ] **Step 1: Implement endpoint filter** (mirror `RequireOtAdminExtensions` but accept permission key parameter via route group extension methods `RequireTramitesRead()` and `RequireTramitesCreate()`).

SuperAdmin with Global scope bypasses via `AuthorizationService.CanAccess`.

### Task 3: Traffic authority picker

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Runtime/Ot/TrafficAuthorityPicker.cs`

- [ ] **Step 1: Implement query**

```csharp
// Join company_traffic_authority_matrix (IsEnabled) 
// with ot_profiles (Status=Active, DivipolCode=AuthorityCode)
// for company resolved from JWT tenant_id
```

Return `TrafficAuthorityDto(DivipolCode, DisplayName, OtTenantId)`.

### Task 4: Create handler

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Runtime/Create/TramitesCreateHandler.cs`
- Create: `services/core-api/src/Flit.Procedures.Runtime/Create/TramitesCreateModels.cs`

- [ ] **Step 1: Validate against `IProcedureDefinitionService.GetByCodeAsync` or by id**
- [ ] **Step 2: Verify actor count/labels match definition**
- [ ] **Step 3: Juridica actors require `legalRepresentative` payload**
- [ ] **Step 4: Persist instance + actors (legal rep as child row) in transaction**
- [ ] **Step 5: Return `201` with `{ id, status: "PendienteEnvio" }`**

### Task 5: Index handler

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Runtime/Index/TramitesIndexHandler.cs`

- [ ] **Step 1: Paginate `procedure_instances` ordered by `CreatedAt DESC`**
- [ ] **Step 2: Tenant filter unless SuperAdmin**
- [ ] **Step 3: Map to `TramiteIndexItem` (id short form, type name, ot display, vehicle, status, createdAt)**

### Task 6: Lookup stubs

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Runtime/Lookups/IExternalLookupService.cs`
- Create: `services/core-api/src/Flit.Procedures.Runtime/Lookups/StubExternalLookupService.cs`

- [ ] **Step 1: RUES returns `{ razonSocial: "Empresa Demo S.A.S.", estado: "ACTIVA" }`**
- [ ] **Step 2: SIMIT/RNMC return empty arrays**

### Task 7: Endpoints + Program.cs

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Runtime/Endpoints/TramitesEndpoints.cs`
- Modify: `services/core-api/src/Flit.Identity.Api/Program.cs`

- [ ] **Step 1: Map routes per spec section 3**
- [ ] **Step 2: Register handlers in DI**

### Task 8: Integration tests

**Files:**
- Create: `TramitesCreateTests.cs`, `TramitesIndexTests.cs`, `TramitesAuthTests.cs`

- [ ] **Step 1: Seed active procedure type via admin API (existing test helper pattern)**
- [ ] **Step 2: Test happy path create with natural actors**
- [ ] **Step 3: Test jurídica + legal representative**
- [ ] **Step 4: Test operator without permission gets 403**
- [ ] **Step 5: Run `dotnet test --filter Tramites`**

---

## HU-3: Upload PDF (#10073)

**Branch:** `feature/HU10073-DCHICA-api-tramites-documents`

### Task 1: Document storage

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Runtime/Documents/ProcedureDocumentStorage.cs`
- Modify: `docker-compose.yml`

- [ ] **Step 1: Implement `SaveAsync(Guid instanceId, string label, Stream content, string fileName)`**
- [ ] **Step 2: Path pattern `{basePath}/{tenantId}/{instanceId}/{label}.pdf`**
- [ ] **Step 3: Add volume mount and env `Procedures__DocumentStoragePath=/data/procedure-docs`**

### Task 2: Upload handler

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Runtime/Documents/TramitesDocumentUploadHandler.cs`

- [ ] **Step 1: Accept `IFormFile`, validate `ContentType == application/pdf` OR magic bytes `%PDF`**
- [ ] **Step 2: Verify label exists in definition as Static**
- [ ] **Step 3: Upsert `procedure_instance_documents` row**
- [ ] **Step 4: Map `POST /{id:guid}/documents?label={label}`**

### Task 3: Upload test

**Files:**
- Create: `TramitesDocumentUploadTests.cs`

- [ ] **Step 1: Upload valid PDF → 200**
- [ ] **Step 2: Upload `.txt` renamed → 400**

---

## HU-4: FE grilla (#10074)

**Branch:** `feature/HU10074-DCHICA-fe-tramites-index`

### Task 1: Dev seed permissions

**Files:**
- Modify: `DevTenantSeeder.cs`

- [ ] **Step 1: Add `tramites:read` and `tramites:create` to `TenantAdminPermissionKeys`**

### Task 2: API client + page

**Files:**
- Create: `frontend/lib/tramites/types.ts`, `api.ts`
- Create: `frontend/app/tramites/layout.tsx`, `page.tsx`
- Create: `frontend/components/tramites/TramitesIndexTable.tsx`
- Modify: `frontend/lib/flit/nav.ts`

- [ ] **Step 1: Layout redirects if no `tramites:read`**
- [ ] **Step 2: Page fetches `/api/v1/tramites/index` server-side**
- [ ] **Step 3: Table shows columns per spec; empty state message**
- [ ] **Step 4: Nav adds Trámites link**

### Task 3: E2E

**Files:**
- Create: `frontend/e2e/tramites/tramites-index.spec.ts`

- [ ] **Step 1: Login as tenant admin, assert `/tramites` visible and grid renders**

---

## HU-5: FE wizard (#10075)

**Branch:** `feature/HU10075-DCHICA-fe-tramites-wizard`

### Task 1: Wizard shell

**Files:**
- Create: `frontend/components/tramites/ProcedureInstanceWizard.tsx`
- Modify: `frontend/app/tramites/page.tsx`

- [ ] **Step 1: Modal with 6 steps, step indicator, back/next**
- [ ] **Step 2: "Nuevo trámite" button visible only with `tramites:create`**

### Task 2: Steps 1–3

- [ ] **Step 1: Step 1 — fetch `/tramites/procedure-types`, select triggers load definition**
- [ ] **Step 2: Step 2 — fetch `/tramites/traffic-authorities`, radio list**
- [ ] **Step 3: Step 3 — input placa/VIN based on `vehicleQueryMode`, consult RUNT via `/api/v1/runt/{type}?q=`**

### Task 3: Step 4 — Actors

- [ ] **Step 1: Render one form per actor from definition**
- [ ] **Step 2: Person kind toggle; jurídica locks doc type to NIT**
- [ ] **Step 3: On jurídica, show legal representative sub-form (CC/CE/Pasaporte + número)**
- [ ] **Step 4: Consult buttons call lookup stubs + RUNT**

### Task 4: Steps 5–6

- [ ] **Step 1: Step 5 — file input per static document; client-side PDF check**
- [ ] **Step 2: Step 6 — summary review**
- [ ] **Step 3: Submit flow: POST create → upload each PDF → close modal → refresh**

### Task 5: E2E create

**Files:**
- Create: `frontend/e2e/tramites/tramites-create.spec.ts`

- [ ] **Step 1: Full wizard with seeded procedure type and OT**

---

## Post-implementation checklist

- [ ] All integration tests pass: `dotnet test tests/Flit.Identity.IntegrationTests`
- [ ] Frontend lint: `cd frontend && pnpm lint`
- [ ] E2E: `cd frontend && pnpm exec playwright test e2e/tramites`
- [ ] Update `docs/postman/` collection with tramites endpoints (optional)
