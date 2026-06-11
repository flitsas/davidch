# Admin OT — Organismos de Tránsito (MVP)

**Date:** 2026-06-11  
**Status:** Approved  
**ADO Feature:** [#9558](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9558) — [ADMIN-OT] Administración de Organismos de Tránsito e Inteligencia Documental  
**ADO User Stories:** [#9820](https://dev.azure.com/FlitDevOps/_workitems/edit/9820)–[#9824](https://dev.azure.com/FlitDevOps/_workitems/edit/9824) (HU-1..HU-4 MVP)  
**Project:** FLIT 2.0 — FLIT EVOLUTION  
**Depends on:** [Identity JWT/RBAC design](./2026-06-09-identity-jwt-rbac-design.md) (Feature #9548, implemented), [Admin Compañías design](./2026-06-11-admin-companies-governance-design.md) (Feature #9551, implemented)

## Summary

Design the **Admin OT** MVP: SuperAdmin provisioning of Organismos de Tránsito (OT) tenants, persistence of Dashboard vs QX integration mode, and tenant-scoped document precedence configuration with drag-and-drop UI.

This spec covers the **acotado MVP** for Feature #9558 — provisioning, RF02, RF09, and RF10. Quipux webhooks, business rules engine, unified Trámites menu, and QX UI suppression are deferred to post-MVP phases.

## Locked decisions

| Area | Decision |
|------|----------|
| Module placement | New `Flit.OT.*` module in existing .NET modular monolith at `services/core-api/` (same deployable as Identity) |
| OT vs tenant | **`ot_profiles.tenant_id → tenants.id`** (1:1 UNIQUE FK) |
| OT vs company | **Separate** from `companies` — OT profile is independent of B2B company governance |
| Provisioning | **Both modes:** atomic create (tenant + OT + default order items) OR link existing tenant |
| SuperAdmin auth | `/api/v1/admin/ot/*` — `RequireSuperAdmin()` (same pattern as Companies) |
| OT Admin auth | `/api/v1/ot/settings/*` — JWT tenant match + `tramites:update` (Tenant scope) |
| Integration mode | Enum `Dashboard` (native FLIT) \| `Qx` (external Quipux); persisted on `ot_profiles` |
| Document order | Per `(tenant_id, procedure_type_code)`; positions 1..N contiguous for included items |
| Frontend SuperAdmin | Next.js App Router under `/admin/ot/*` |
| Frontend OT Admin | `/ot/settings/integracion`, `/ot/settings/documentos` |
| DnD library | `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities` |
| API prefix | `/api/v1/admin/ot/*` (admin), `/api/v1/ot/settings/*` (OT Admin) |
| Database | PostgreSQL shared schema; `OtDbContext` with FK to `tenants` |
| Package manager | pnpm monorepo at repo root (`@flit/frontend`) |

## Non-goals (MVP)

- Unified Trámites sidebar / Dashboard + QX queue (RF01)
- Hide Approve/Reject buttons in QX mode (RF03)
- Quipux webhooks, hot-update sync, integration logs (RF04, RF05)
- Business rules hot-swapping engine (RF08)
- Custom document label CRUD with safe queue exclusion (RF11)
- PDF consolidated generation (consumer in Trámites module — interfaces only)

---

## 1. Architecture overview

### System diagram

```
Browser
   │
   ▼
YARP Gateway
   ├── /api/v1/admin/ot/*        ──► Flit.OT.Admin (SuperAdmin)
   ├── /api/v1/ot/settings/*     ──► Flit.OT.Admin (OT Admin)
   ├── /api/v1/admin/companies/* ──► Flit.Companies (unchanged)
   ├── /api/auth|users|roles/*    ──► Flit.Identity (unchanged)
   └── /admin/ot/* | /ot/settings/* ──► Next.js 16 (@flit/frontend)
                                          │
                                          ▼
                                     PostgreSQL (shared)
```

### Module layout

| Project | Responsibility |
|---------|----------------|
| `Flit.OT.Shared` | Enums, runtime DTOs, public interfaces for Trámites module |
| `Flit.OT.Infrastructure` | EF Core entities, `OtDbContext`, migrations, seeders |
| `Flit.OT.Admin` | Minimal APIs, handlers, SuperAdmin + OT Admin guards |

Registered in `Flit.Identity.Api/Program.cs` alongside `CompaniesDbContext`.

### Runtime interfaces (for future Trámites)

```csharp
public interface IOtIntegrationModeService
{
    Task<IntegrationMode> GetModeAsync(Guid tenantId, CancellationToken ct);
}

public interface IOtDocumentOrderService
{
    Task<IReadOnlyList<DocumentOrderItemDto>> GetIncludedOrderAsync(
        Guid tenantId, string procedureTypeCode, CancellationToken ct);
}
```

Implemented in `Flit.OT.Admin/Services/`; registered in DI; consumed in-process.

### Cross-cutting concerns

1. **Authentication** — reuse Identity JWT cookie middleware (`flit_access`)
2. **SuperAdmin authorization** — `is_super_admin == true`; `403 Forbidden`
3. **OT Admin authorization** — resolve `tenant_id` from JWT; `AuthorizationService.CanAccess(user, "tramites:update", new ResourceContext(tenantId, userId))`; OT profile must exist for tenant
4. **Tenant sync** — suspend/activate OT mirrors `tenants.is_active` in same transaction
5. **Audit** — config mutations set `updated_at` + `updated_by` on order items

---

## 2. Data model

### Entity relationship

```
tenants (Identity)              ot_profiles
├── id ◄──────────────────────── tenant_id (FK, UNIQUE)
├── name                          ├── id (PK)
├── slug                          ├── divipol_code (UNIQUE)
├── is_active                     ├── display_name
└── ...                           ├── integration_mode (Dashboard | Qx)
                                  ├── status (Active | Suspended)
                                  ├── created_at
                                  └── updated_at

procedure_type_catalog (seed)
document_type_catalog (seed)
procedure_document_defaults (seed) ──► default matrix per procedure

ot_document_order_items
├── tenant_id (FK)
├── procedure_type_code (FK catalog)
├── document_type_code (FK catalog)
├── position (int)
├── is_included (bool)
├── updated_at
└── updated_by (nullable UUID)
```

**Invariants:**

- One `ot_profiles` row per tenant (UNIQUE on `tenant_id`).
- `divipol_code` unique platform-wide.
- A tenant may exist without an OT profile (B2B-only or dev tenants).
- Order items seeded from `procedure_document_defaults` on OT create/link.

### Seed catalogs (MVP)

**procedure_type_catalog**

| code | name |
|------|------|
| `MATRICULA_INICIAL` | Matrícula inicial |
| `TRASPASO` | Traspaso de propiedad |
| `RADICADO_CUENTA` | Radicado de cuenta |

**document_type_catalog**

| code | default_sort_order |
|------|-------------------|
| `CEDULA` | 1 |
| `TARJETA_PROPIEDAD` | 2 |
| `SOAT` | 3 |
| `RTM` | 4 |
| `FACTURA` | 5 |

**procedure_document_defaults** — link valid document subsets per procedure (all five for `MATRICULA_INICIAL`; subsets for others).

---

## 3. API design

### SuperAdmin — `/api/v1/admin/ot`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/index` | Paginated grid; filters: `divipol`, `name`, `id`, `auditFrom`, `auditTo` |
| POST | `/` | Create (mode `create` \| `link`) |
| GET | `/{id}` | OT detail |
| PATCH | `/{id}` | Update profile fields |
| PATCH | `/{id}/status` | Active/Suspended (+ tenant sync) |
| GET | `/{id}/config/integration` | RF02 read |
| PUT | `/{id}/config/integration` | RF02 write `{ integration_mode: "Dashboard" \| "Qx" }` |
| GET | `/{id}/document-order/{procedureCode}` | RF09 read |
| PUT | `/{id}/document-order/{procedureCode}` | RF10 write `{ items: [{ document_type_code, position, is_included }] }` |

**Create request (link mode):**

```json
{
  "mode": "link",
  "tenant_id": "uuid",
  "divipol_code": "11001000",
  "display_name": "OT Bogotá",
  "status": "Active"
}
```

**Create request (create mode):** adds `slug` for new tenant.

**Conflicts:** `409` — duplicate `divipol_code`, tenant already linked, slug taken.

### OT Admin — `/api/v1/ot/settings`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/config/integration` | RF02 (tenant from JWT) |
| PUT | `/config/integration` | RF02 |
| GET | `/procedure-types` | Catalog for UI selector |
| GET | `/document-order/{procedureCode}` | RF09 |
| PUT | `/document-order/{procedureCode}` | RF10 |

**Validation (document order PUT):**

- All codes must exist in `procedure_document_defaults` for the procedure.
- No duplicate `document_type_code` in payload.
- Included items: contiguous positions starting at 1.
- Excluded items: `is_included=false`, row retained.

**Performance:** reorder PUT completes in < 500 ms (integration test assertion).

---

## 4. Frontend design

### SuperAdmin routes

| Route | Purpose |
|-------|---------|
| `/admin/ot` | Index grid + filters |
| `/admin/ot/new` | Create/link wizard |
| `/admin/ot/[id]?tab=perfil\|integracion\|documentos` | Tabbed editor |

### OT Admin routes

| Route | Purpose |
|-------|---------|
| `/ot/settings/integracion` | Dashboard/QX switch |
| `/ot/settings/documentos` | Procedure selector + DnD list |

### Navigation

- SuperAdmin: add `{ href: "/admin/ot", label: "Organismos de tránsito" }` in `buildAdminNav`
- OT Admin: add `{ href: "/ot/settings/integracion", label: "Configuración OT" }` when `hasPermission(session, "tramites:update")`

### Shared component

`components/ot/DocumentOrderList.tsx` — `@dnd-kit` sortable list with include toggle; used by SuperAdmin Documentos tab and OT Admin settings.

---

## 5. Testing strategy

### Integration tests (`services/core-api/tests/Flit.Identity.IntegrationTests/Ot/`)

| Test class | Coverage |
|------------|----------|
| `OtMigrationTests` | Schema + catalog seed |
| `OtIndexTests` | SuperAdmin index; tenant admin forbidden |
| `OtCrudTests` | Create/link, conflicts, default order seed |
| `OtIntegrationModeTests` | Dashboard/QX round-trip |
| `OtDocumentOrderTests` | Reorder < 500 ms, validation, exclude retains row |
| `OtSettingsAuthTests` | OT Admin auth + forbidden cases |

Uses existing `IdentityWebApplicationFactory` + Testcontainers PostgreSQL. CI generates JWT keys before `dotnet test`.

### E2E (`frontend/e2e/identity/`)

| Spec | Flow |
|------|------|
| `ot-index.spec.ts` | SuperAdmin grid visible |
| `ot-create.spec.ts` | Create OT via wizard |
| `ot-document-order.spec.ts` | DnD reorder persists |

### Dev seed change

Add `tramites:update` (Tenant scope) to `TenantA-Admin` role in `DevTenantSeeder`.

---

## 6. Implementation phases (ADO mapping)

| Phase | ADO | Deliverable | Branch |
|-------|-----|-------------|--------|
| HU-1 | #9820 | Schema, catalog seed, index API | `feature/HU9820-DCHICA-schema-ot-catalog-index` |
| HU-2 | #9821 | CRUD SuperAdmin + grid/wizard FE | `feature/HU9821-DCHICA-ot-crud-superadmin-fe` |
| HU-3 | #9822 | Integration mode BE + FE | `feature/HU9822-DCHICA-ot-integration-mode` |
| HU-4 | #9824 | Document order API + DnD UI | `feature/HU9824-DCHICA-ot-document-order-dnd` |

**Dependency order:** HU-1 → HU-2 → HU-3 → HU-4. Pause for review between HUs per `flit-hu-story-git-workflow`.

---

## 7. RF coverage matrix

| RF | MVP | Notes |
|----|-----|-------|
| RF01 | ❌ | Trámites menu consolidation — post-MVP |
| RF02 | ✅ | Integration mode Dashboard/QX |
| RF03 | ❌ | Hide Approve/Reject in QX mode — post-MVP |
| RF04 | ❌ | Quipux webhooks — post-MVP |
| RF05 | ❌ | Integration logs — post-MVP |
| RF08 | ❌ | Rules hot-swapping — post-MVP |
| RF09 | ✅ | Document order tab per procedure |
| RF10 | ✅ | Drag-and-drop + < 500 ms persist |
| RF11 | ❌ | Custom labels — post-MVP |

---

## 8. Post-MVP roadmap

1. Quipux webhook ingress + audit log viewer (RF04, RF05)
2. Frontend middleware hiding approval controls in QX mode (RF03)
3. Unified Trámites dashboard (RF01)
4. Business rules engine (RF08)
5. Custom document labels (RF11)
6. Wire Trámites module to `IOtIntegrationModeService` / `IOtDocumentOrderService`
