# Admin OT MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the Admin OT MVP for Feature #9558 — OT tenant provisioning (create/link), Dashboard/QX integration mode persistence, and document precedence drag-and-drop per procedure type.

**Architecture:** New `Flit.OT.*` module in the existing .NET modular monolith at `services/core-api/`, mirroring `Flit.Companies.*`. OT profiles are 1:1 with Identity tenants. SuperAdmin uses `/api/v1/admin/ot/*`; OT Admins use tenant-scoped `/api/v1/ot/settings/*` with `tramites:update` (Tenant). Next.js pages at `/admin/ot/*` and `/ot/settings/*`.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, EF Core 10 + Npgsql, PostgreSQL 16, Next.js 16, React 19, TypeScript, Tailwind v4, pnpm 9, `@dnd-kit/core` + `@dnd-kit/sortable`, xUnit + Testcontainers, Playwright

**Spec:** `docs/superpowers/specs/2026-06-11-admin-ot-mvp-design.md`

**ADO Feature:** [#9558](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9558)

| Story | ADO | Title | Branch |
|-------|-----|-------|--------|
| HU-1 | #9820 | Schema OT + catalog seed + index API | `feature/HU9820-DCHICA-schema-ot-catalog-index` |
| HU-2 | #9821 | CRUD SuperAdmin + grid/wizard FE | `feature/HU9821-DCHICA-ot-crud-superadmin-fe` |
| HU-3 | #9822 | Integration mode Dashboard/QX | `feature/HU9822-DCHICA-ot-integration-mode` |
| HU-4 | #9824 | Document order drag-and-drop | `feature/HU9824-DCHICA-ot-document-order-dnd` |

**Dependency order:** HU-1 → HU-2 → HU-3 → HU-4. Stop for review between HUs.

---

## File Map

### Backend (`services/core-api/`)

| File | Responsibility |
|------|----------------|
| `Flit.Identity.sln` | Add OT projects |
| `src/Flit.OT.Shared/Domain/IntegrationMode.cs` | `Dashboard`, `Qx` enum |
| `src/Flit.OT.Shared/Domain/OtStatus.cs` | `Active`, `Suspended` enum |
| `src/Flit.OT.Shared/IOtDocumentOrderService.cs` | Runtime interface |
| `src/Flit.OT.Shared/IOtIntegrationModeService.cs` | Runtime interface |
| `src/Flit.OT.Shared/DocumentOrderItemDto.cs` | Runtime DTO |
| `src/Flit.OT.Infrastructure/Persistence/OtDbContext.cs` | EF Core context |
| `src/Flit.OT.Infrastructure/Persistence/Entities/*.cs` | OT entities |
| `src/Flit.OT.Infrastructure/Persistence/Seed/OtDbSeeder.cs` | Catalog seed |
| `src/Flit.OT.Infrastructure/Persistence/OtDefaultOrderFactory.cs` | Seed order on create |
| `src/Flit.OT.Infrastructure/Migrations/*` | EF migrations |
| `src/Flit.OT.Admin/Auth/RequireSuperAdminExtensions.cs` | Copy from Companies |
| `src/Flit.OT.Admin/Auth/RequireOtAdminExtensions.cs` | Tenant + `tramites:update` |
| `src/Flit.OT.Admin/Index/OtIndexHandler.cs` | Paginated grid |
| `src/Flit.OT.Admin/Crud/OtCrudHandler.cs` | Create/link/update/status |
| `src/Flit.OT.Admin/Config/OtIntegrationHandler.cs` | RF02 |
| `src/Flit.OT.Admin/Config/OtDocumentOrderHandler.cs` | RF09/10 |
| `src/Flit.OT.Admin/Settings/OtSettingsHandler.cs` | Tenant-scoped facade |
| `src/Flit.OT.Admin/Services/OtDocumentOrderService.cs` | Runtime impl |
| `src/Flit.OT.Admin/Services/OtIntegrationModeService.cs` | Runtime impl |
| `src/Flit.OT.Admin/Endpoints/OtAdminEndpoints.cs` | SuperAdmin routes |
| `src/Flit.OT.Admin/Endpoints/OtSettingsEndpoints.cs` | OT Admin routes |
| `src/Flit.Identity.Api/Program.cs` | Register OtDbContext, handlers, endpoints |
| `src/Flit.Identity.Api/Flit.Identity.Api.csproj` | Project refs |
| `src/Flit.Identity.Infrastructure/Persistence/Seed/DevTenantSeeder.cs` | Add `tramites:update` |

### Frontend (`frontend/`)

| File | Responsibility |
|------|----------------|
| `lib/admin/ot-types.ts` | DTO types |
| `lib/admin/ot-api.ts` | SuperAdmin API helpers |
| `lib/ot/settings-api.ts` | OT Admin API helpers |
| `lib/flit/nav.ts` | Nav items |
| `app/admin/ot/layout.tsx` | SuperAdmin guard |
| `app/admin/ot/page.tsx` | Index grid |
| `app/admin/ot/new/page.tsx` | Create wizard |
| `app/admin/ot/[id]/page.tsx` | Tabbed editor |
| `components/admin/OtCreateForm.tsx` | Create/link form |
| `components/admin/OtIndexFilters.tsx` | Grid filters |
| `components/admin/OtTabs.tsx` | Tab shell |
| `components/admin/ot-tabs/PerfilTab.tsx` | Profile + status |
| `components/admin/ot-tabs/IntegracionTab.tsx` | Dashboard/QX |
| `components/admin/ot-tabs/DocumentosTab.tsx` | Procedure + DnD |
| `components/ot/DocumentOrderList.tsx` | Shared DnD list |
| `app/ot/settings/layout.tsx` | OT Admin guard |
| `app/ot/settings/integracion/page.tsx` | OT Admin integration |
| `app/ot/settings/documentos/page.tsx` | OT Admin documents |
| `e2e/identity/ot-index.spec.ts` | E2E grid |
| `e2e/identity/ot-create.spec.ts` | E2E create |
| `e2e/identity/ot-document-order.spec.ts` | E2E DnD |

### Tests (`services/core-api/tests/`)

| File | Responsibility |
|------|----------------|
| `Flit.Identity.IntegrationTests/Database/OtMigrationTests.cs` | Schema + seed |
| `Flit.Identity.IntegrationTests/Ot/OtIndexTests.cs` | Index API |
| `Flit.Identity.IntegrationTests/Ot/OtCrudTests.cs` | CRUD |
| `Flit.Identity.IntegrationTests/Ot/OtIntegrationModeTests.cs` | RF02 |
| `Flit.Identity.IntegrationTests/Ot/OtDocumentOrderTests.cs` | RF10 |
| `Flit.Identity.IntegrationTests/Ot/OtSettingsAuthTests.cs` | OT Admin auth |

---

## HU-1: Schema OT + catalog seed + index API

**Branch:** `feature/HU9820-DCHICA-schema-ot-catalog-index`

### Task 1: OT shared enums and project scaffold

**Files:**
- Create: `services/core-api/src/Flit.OT.Shared/Flit.OT.Shared.csproj`
- Create: `services/core-api/src/Flit.OT.Shared/Domain/IntegrationMode.cs`
- Create: `services/core-api/src/Flit.OT.Shared/Domain/OtStatus.cs`
- Create: `services/core-api/src/Flit.OT.Infrastructure/Flit.OT.Infrastructure.csproj`
- Create: `services/core-api/src/Flit.OT.Admin/Flit.OT.Admin.csproj`
- Modify: `services/core-api/Flit.Identity.sln`
- Modify: `services/core-api/src/Flit.Identity.Api/Flit.Identity.Api.csproj`

- [ ] **Step 1: Create Shared project**

```bash
cd /home/david/davidch/services/core-api/src
dotnet new classlib -n Flit.OT.Shared -o Flit.OT.Shared -f net10.0
```

`Domain/IntegrationMode.cs`:

```csharp
namespace Flit.OT.Shared.Domain;

public enum IntegrationMode
{
    Dashboard = 0,
    Qx = 1
}
```

`Domain/OtStatus.cs`:

```csharp
namespace Flit.OT.Shared.Domain;

public enum OtStatus
{
    Active = 0,
    Suspended = 1
}
```

- [ ] **Step 2: Create Infrastructure project** — mirror `Flit.Companies.Infrastructure.csproj` package refs (EF Core, Npgsql) + project ref to Shared and Identity.Infrastructure.

- [ ] **Step 3: Create Admin project** — refs: OT.Infrastructure, OT.Shared, Identity.Shared, Identity.Rbac, Identity.Infrastructure.

- [ ] **Step 4: Add projects to `Flit.Identity.sln` and reference from `Flit.Identity.Api.csproj`**

- [ ] **Step 5: Verify build**

```bash
cd /home/david/davidch/services/core-api
dotnet build Flit.Identity.sln
```

Expected: `Build succeeded`

- [ ] **Step 6: Commit**

```bash
git add services/core-api/src/Flit.OT.* services/core-api/Flit.Identity.sln services/core-api/src/Flit.Identity.Api/Flit.Identity.Api.csproj
git commit -m "feat(ot): scaffold Flit.OT shared, infrastructure, and admin projects"
```

---

### Task 2: EF entities and OtDbContext

**Files:**
- Create: `services/core-api/src/Flit.OT.Infrastructure/Persistence/Entities/OtProfile.cs`
- Create: `services/core-api/src/Flit.OT.Infrastructure/Persistence/Entities/ProcedureTypeCatalog.cs`
- Create: `services/core-api/src/Flit.OT.Infrastructure/Persistence/Entities/DocumentTypeCatalog.cs`
- Create: `services/core-api/src/Flit.OT.Infrastructure/Persistence/Entities/ProcedureDocumentDefault.cs`
- Create: `services/core-api/src/Flit.OT.Infrastructure/Persistence/Entities/OtDocumentOrderItem.cs`
- Create: `services/core-api/src/Flit.OT.Infrastructure/Persistence/OtDbContext.cs`
- Create: `services/core-api/tests/Flit.Identity.IntegrationTests/Database/OtMigrationTests.cs`
- Modify: `services/core-api/src/Flit.Identity.Api/Program.cs`
- Modify: `services/core-api/tests/Flit.Identity.IntegrationTests/Flit.Identity.IntegrationTests.csproj`

- [ ] **Step 1: Write failing migration test**

```csharp
using Flit.OT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Database;

public class OtMigrationTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;
    public OtMigrationTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Migration_creates_ot_schema_with_tenant_fk()
    {
        if (!_factory.IsDockerAvailable) return;

        using var scope = _factory.Services.CreateScope();
        var otDb = scope.ServiceProvider.GetRequiredService<OtDbContext>();

        var tables = await otDb.Database.SqlQueryRaw<TableRow>(
            """
            SELECT table_name AS "TableName"
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            """
        ).ToListAsync();

        var names = tables.Select(t => t.TableName).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("ot_profiles", names);
        Assert.Contains("procedure_type_catalog", names);
        Assert.Contains("document_type_catalog", names);
        Assert.Contains("procedure_document_defaults", names);
        Assert.Contains("ot_document_order_items", names);
    }

    private sealed record TableRow(string TableName);
}
```

- [ ] **Step 2: Run test — expect FAIL** (OtDbContext not registered)

```bash
cd /home/david/davidch/services/core-api
dotnet test Flit.Identity.sln --filter "FullyQualifiedName~OtMigrationTests"
```

- [ ] **Step 3: Implement entities and `OtDbContext`** — snake_case table names; UNIQUE on `ot_profiles.tenant_id` and `ot_profiles.divipol_code`; FK `tenant_id → tenants.id`.

- [ ] **Step 4: Register in `Program.cs`**

```csharp
builder.Services.AddDbContext<OtDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Identity")));

// startup block:
var otDb = scope.ServiceProvider.GetRequiredService<OtDbContext>();
await otDb.Database.MigrateAsync();
await OtDbSeeder.SeedAsync(otDb);
```

- [ ] **Step 5: Add migration**

```bash
cd /home/david/davidch/services/core-api/src/Flit.Identity.Api
dotnet ef migrations add InitialOtSchema \
  --project ../Flit.OT.Infrastructure/Flit.OT.Infrastructure.csproj \
  --context OtDbContext \
  --output-dir Migrations
```

- [ ] **Step 6: Add OT.Infrastructure ref to integration test csproj; run test — expect PASS**

- [ ] **Step 7: Commit**

```bash
git commit -m "feat(ot): add EF schema and initial migration for OT module"
```

---

### Task 3: Catalog seed data

**Files:**
- Create: `services/core-api/src/Flit.OT.Infrastructure/Persistence/Seed/OtDbSeeder.cs`
- Modify: `services/core-api/tests/Flit.Identity.IntegrationTests/Database/OtMigrationTests.cs`

- [ ] **Step 1: Add seed test**

```csharp
[Fact]
public async Task Seed_creates_procedure_and_document_catalogs()
{
    if (!_factory.IsDockerAvailable) return;

    using var scope = _factory.Services.CreateScope();
    var otDb = scope.ServiceProvider.GetRequiredService<OtDbContext>();

    var procedures = await otDb.ProcedureTypeCatalog.AsNoTracking().ToListAsync();
    Assert.Contains(procedures, p => p.Code == "MATRICULA_INICIAL");

    var documents = await otDb.DocumentTypeCatalog.AsNoTracking().ToListAsync();
    Assert.True(documents.Count >= 5);
}
```

- [ ] **Step 2: Implement idempotent `OtDbSeeder.SeedAsync`** per spec §2 seed tables.

- [ ] **Step 3: Run tests — expect PASS**

- [ ] **Step 4: Commit**

```bash
git commit -m "feat(ot): seed procedure and document type catalogs"
```

---

### Task 4: Index API + SuperAdmin guard

**Files:**
- Create: `services/core-api/src/Flit.OT.Admin/Auth/RequireSuperAdminExtensions.cs`
- Create: `services/core-api/src/Flit.OT.Admin/Index/OtIndexModels.cs`
- Create: `services/core-api/src/Flit.OT.Admin/Index/OtIndexHandler.cs`
- Create: `services/core-api/src/Flit.OT.Admin/Endpoints/OtAdminEndpoints.cs`
- Modify: `services/core-api/src/Flit.Identity.Api/Program.cs`
- Create: `services/core-api/tests/Flit.Identity.IntegrationTests/Ot/OtIndexTests.cs`

- [ ] **Step 1: Write failing index tests**

```csharp
[Fact]
public async Task SuperAdmin_can_query_ot_index()
{
    if (!_factory.IsDockerAvailable) return;
    var client = await _factory.LoginAsSuperAdminAsync();
    var response = await client.GetAsync("/api/v1/admin/ot/index?page=1&pageSize=20");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}

[Fact]
public async Task Tenant_admin_gets_forbidden_on_ot_index()
{
    if (!_factory.IsDockerAvailable) return;
    var client = await _factory.LoginAsTenantAdminAsync();
    var response = await client.GetAsync("/api/v1/admin/ot/index");
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

- [ ] **Step 2: Copy `RequireSuperAdminExtensions` from Companies; implement `OtIndexHandler`** mirroring `CompanyIndexHandler`.

- [ ] **Step 3: Wire `MapOtAdminEndpoints`** at `/api/v1/admin/ot` with `.RequireSuperAdmin()`.

- [ ] **Step 4: Register handler in DI; run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(ot): add SuperAdmin OT index API"
```

**HU-1 complete — STOP for review.**

---

## HU-2: SuperAdmin OT CRUD + grid frontend

**Branch:** `feature/HU9821-DCHICA-ot-crud-superadmin-fe`

### Task 5: OT CRUD handler

**Files:**
- Create: `services/core-api/src/Flit.OT.Admin/Crud/OtCrudModels.cs`
- Create: `services/core-api/src/Flit.OT.Admin/Crud/OtCrudHandler.cs`
- Create: `services/core-api/src/Flit.OT.Infrastructure/Persistence/OtDefaultOrderFactory.cs`
- Modify: `services/core-api/src/Flit.OT.Admin/Endpoints/OtAdminEndpoints.cs`
- Create: `services/core-api/tests/Flit.Identity.IntegrationTests/Ot/OtCrudTests.cs`

- [ ] **Step 1: Write failing create test (link mode)** — POST `/api/v1/admin/ot` with `mode: "link"`; assert `201` and `ot_document_order_items` seeded.

- [ ] **Step 2: Implement `OtCrudHandler`** — mirror `CompanyCrudHandler`:
  - `create`: new Tenant + OtProfile + `OtDefaultOrderFactory.SeedOrderItems`
  - `link`: validate tenant exists, no existing profile
  - `409` on duplicate divipol or tenant

- [ ] **Step 3: Add tests for duplicate divipol and duplicate tenant**

- [ ] **Step 4: Implement GetDetail, Update, UpdateStatus** (sync `tenants.is_active`)

- [ ] **Step 5: Run tests — expect PASS; commit**

```bash
git commit -m "feat(ot): add OT CRUD with atomic provisioning and default order seed"
```

### Task 6: SuperAdmin frontend — index + create wizard

**Files:**
- Create: `frontend/lib/admin/ot-types.ts`, `ot-api.ts`
- Create: `frontend/app/admin/ot/layout.tsx`, `page.tsx`, `new/page.tsx`
- Create: `frontend/components/admin/OtCreateForm.tsx`, `OtIndexFilters.tsx`
- Modify: `frontend/lib/flit/nav.ts`
- Create: `frontend/e2e/identity/ot-index.spec.ts`, `ot-create.spec.ts`

- [ ] **Step 1: Add nav item** for SuperAdmin in `buildAdminNav`

- [ ] **Step 2: Index page** — mirror `app/admin/companies/page.tsx`; columns: divipol, displayName, status, updatedAt

- [ ] **Step 3: Create wizard** — mirror `CompanyCreateForm`; POST `/api/v1/admin/ot`

- [ ] **Step 4: E2E tests** — mirror companies specs

- [ ] **Step 5: Verify build**

```bash
cd /home/david/davidch && pnpm --filter @flit/frontend build
```

- [ ] **Step 6: Commit**

```bash
git commit -m "feat(ot): add SuperAdmin OT index and create wizard UI"
```

### Task 7: OT editor shell + profile tab

**Files:**
- Create: `frontend/app/admin/ot/[id]/page.tsx`
- Create: `frontend/components/admin/OtTabs.tsx`
- Create: `frontend/components/admin/ot-tabs/PerfilTab.tsx`

- [ ] **Step 1: Tab shell** with `?tab=perfil|integracion|documentos`

- [ ] **Step 2: PerfilTab** — edit divipol, display_name; PATCH status

- [ ] **Step 3: Build + commit**

```bash
git commit -m "feat(ot): add SuperAdmin OT editor shell and profile tab"
```

**HU-2 complete — STOP for review.**

---

## HU-3: Integration mode Dashboard/QX

**Branch:** `feature/HU9822-DCHICA-ot-integration-mode`

### Task 8: Integration mode API

**Files:**
- Create: `OtIntegrationHandler`, `RequireOtAdminExtensions`, `OtSettingsHandler`, `OtSettingsEndpoints`, `OtIntegrationModeService`, `IOtIntegrationModeService`
- Modify: `DevTenantSeeder.cs` — add `tramites:update` to TenantA-Admin
- Create: `OtIntegrationModeTests.cs`, `OtSettingsAuthTests.cs`

- [ ] **Step 1: Seed `tramites:update` permission** on TenantA-Admin role

- [ ] **Step 2: Write failing PUT/GET integration mode test** on `/api/v1/admin/ot/{id}/config/integration`

- [ ] **Step 3: Implement handler + OT Admin settings routes** at `/api/v1/ot/settings/config/integration`

- [ ] **Step 4: Auth tests** — tenant admin with permission succeeds; operator without `tramites:update` gets 403

- [ ] **Step 5: Register `IOtIntegrationModeService`; run tests; commit**

```bash
git commit -m "feat(ot): add integration mode API for SuperAdmin and OT Admin"
```

### Task 9: Integration mode frontend

**Files:**
- Create: `frontend/lib/ot/settings-api.ts`
- Create: `frontend/components/admin/ot-tabs/IntegracionTab.tsx`
- Create: `frontend/app/ot/settings/layout.tsx`, `integracion/page.tsx`
- Modify: `frontend/lib/flit/nav.ts`

- [ ] **Step 1: OT Admin nav** when `hasPermission(session, "tramites:update")`

- [ ] **Step 2: IntegracionTab + OT Admin page** — radio Dashboard vs QX

- [ ] **Step 3: Build + commit**

```bash
git commit -m "feat(ot): add integration mode UI for SuperAdmin and OT Admin"
```

**HU-3 complete — STOP for review.**

---

## HU-4: Document precedence drag-and-drop

**Branch:** `feature/HU9824-DCHICA-ot-document-order-dnd`

### Task 10: Document order API

**Files:**
- Create: `OtDocumentOrderHandler`, `OtDocumentOrderModels`, `IOtDocumentOrderService`, `OtDocumentOrderService`
- Modify: admin + settings endpoints
- Create: `OtDocumentOrderTests.cs`

- [ ] **Step 1: Write failing reorder test** — swap two items; assert `200` and elapsed < 500 ms

- [ ] **Step 2: Implement validation** — contiguous positions, valid codes, exclude retains row

- [ ] **Step 3: Wire admin + settings routes; add `GET /api/v1/ot/settings/procedure-types`**

- [ ] **Step 4: Run tests; commit**

```bash
git commit -m "feat(ot): add document order API with validation and runtime service"
```

### Task 11: Drag-and-drop frontend

**Files:**
- Create: `frontend/components/ot/DocumentOrderList.tsx`
- Create: `frontend/components/admin/ot-tabs/DocumentosTab.tsx`
- Create: `frontend/app/ot/settings/documentos/page.tsx`
- Modify: `frontend/package.json` — add `@dnd-kit/*`
- Create: `frontend/e2e/identity/ot-document-order.spec.ts`

- [ ] **Step 1: Install dnd-kit**

```bash
cd /home/david/davidch/frontend && pnpm add @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities
```

- [ ] **Step 2: Implement `DocumentOrderList`** with sortable rows + include toggle

- [ ] **Step 3: DocumentosTab + OT Admin page**

- [ ] **Step 4: E2E — drag item, reload, verify order**

- [ ] **Step 5: Full verification**

```bash
cd /home/david/davidch/services/core-api && dotnet test Flit.Identity.sln --filter "FullyQualifiedName~Ot"
cd /home/david/davidch && pnpm --filter @flit/frontend build
```

- [ ] **Step 6: Commit**

```bash
git commit -m "feat(ot): add document order drag-and-drop UI for SuperAdmin and OT Admin"
```

**HU-4 complete — STOP for review.**

---

## Post-implementation checklist

- [ ] Update spec status to `Approved` in design doc
- [ ] Register PRs via `flit-integration-ado` skill (Modo A)
- [ ] Optional: Postman collection `docs/postman/FLIT-Admin-OT-API.postman_collection.json`

---

## Plan self-review (spec coverage)

| Spec section | Task |
|--------------|------|
| §1 Architecture `Flit.OT.*` | Task 1 |
| §2 Data model + seed | Tasks 2–3 |
| §3 Provisioning create/link | Task 5 |
| §4 Admin API index/CRUD/config | Tasks 4, 5, 8, 10 |
| §4 Settings API | Tasks 8, 10 |
| §4 Frontend admin | Tasks 6–7, 9, 11 |
| §5 Testing | Tasks 2–5, 8, 10, 11 |
| Runtime interfaces | Tasks 8, 10 |
| RF02 | Tasks 8–9 |
| RF09–RF10 | Tasks 10–11 |
| Deferred RF01,03–05,08,11 | Not in plan (spec §8) |
