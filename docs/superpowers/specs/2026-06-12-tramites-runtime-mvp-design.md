# Trámites Runtime — MVP A

**Date:** 2026-06-12  
**Status:** Approved  
**ADO Feature:** [#9733](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9733) — [CREACIÓN-TRÁMITES] Módulo de Trámites Dinámicos  
**ADO User Stories:** [#10071](https://dev.azure.com/FlitDevOps/_workitems/edit/10071)–[#10075](https://dev.azure.com/FlitDevOps/_workitems/edit/10075)  
**Project:** FLIT 2.0 — FLIT EVOLUTION  
**Depends on:** Identity (#9548), Parametrizador (#9555), Admin OT (#9558), Admin Compañías (#9551)

## Summary

Design the **runtime Trámites module (MVP A)**: company operators and SuperAdmin create procedure instances from active parametrized types, select a traffic authority (OT), capture vehicle and actors (with automatic legal-representative sub-step for juridical persons), upload static PDF documents, and persist with status `PendienteEnvio`.

Consumes `IProcedureDefinitionService` (#9555), company traffic-authority matrix and OT profiles (#9551/#9558), and existing RUNT proxy. SIMIT, RUES, and RNMC use dev stubs in MVP A.

## Locked decisions

| Area | Decision |
|------|----------|
| MVP scope | Option A — wizard create + index only; no OT send, dynamic docs, copropiedad, signatures, compound IDs |
| Module | `Flit.Procedures.Runtime` in core-api monolith |
| Database | Extend `ProceduresDbContext` with instance tables |
| API prefix | `/api/v1/tramites/*` |
| Frontend route | `/tramites` (grid + modal wizard) |
| Wizard steps | 6: Tipo → OT → Vehículo → Actores → Documentos PDF → Confirmar |
| OT selection | Before vehicle/actors; enabled company authorities ∩ active `ot_profiles` by `divipol_code` |
| Actors | Semantic roles from definition; person kind at runtime |
| Jurídica | RUES stub + mandatory Representante Legal sub-step (Natural, RUNT/SIMIT/RNMC) |
| Documents | Only `DocumentKind.Static` from definition; PDF only (`application/pdf`) |
| Initial status | `PendienteEnvio` |
| File storage | Local filesystem volume (`ProcedureDocumentStorage`); path in DB |
| Auth read | `tramites:read` (Tenant) or SuperAdmin Global |
| Auth create | `tramites:create` (Tenant) or SuperAdmin Global |
| OT Admin visibility | Deferred — `tramites:update` remains OT settings only |
| #9558 new HUs | **None required** — runtime consumes existing OT/company data |

## Non-goals (MVP A)

- Send procedure to OT (state transition beyond `PendienteEnvio`)
- Dynamic document generation
- Copropiedad 100% validation (RF07 ADO)
- Async signatures / selfie letters (RF08–RF09 ADO)
- Compound display IDs e.g. `TRASP-02_EVE-8841` (RF01 ADO)
- Cross-tenant OT operator grid (RF02 partial)
- Background non-blocking banners (RF05–RF06 ADO)
- SuperMaestro admin commands (`tramites.admin.maestro`)
- Real SIMIT / RUES / RNMC HTTP integrations

---

## 1. Architecture

```
Browser (SuperAdmin | Company operator)
   │
   ▼
YARP Gateway
   ├── /api/v1/tramites/*              → Flit.Procedures.Runtime
   ├── /api/v1/runt/*                  → Flit.Companies.Runt
   ├── /api/v1/admin/procedure-types/* → Flit.Procedures.Admin
   └── /tramites/*                     → Next.js (@flit/frontend)
                                          │
                                          ▼
                                     PostgreSQL + local PDF volume
```

### Module layout

| Project | Responsibility |
|---------|----------------|
| `Flit.Procedures.Runtime` | Instance CRUD, OT picker, lookup stubs, PDF upload |
| `Flit.Procedures.Shared` | Runtime enums/DTOs (`ProcedureStatus`, `PersonKind`, `DocumentIdType`) |
| `Flit.Procedures.Infrastructure` | EF entities + migrations for instances |

Registered in `Flit.Identity.Api/Program.cs` alongside existing Procedures services.

### Cross-cutting

1. **Authentication** — JWT cookie (`flit_access`) via existing middleware
2. **Authorization** — `RequireTramitesPermission("tramites:read" | "tramites:create")`; SuperAdmin bypass via Global scope
3. **Tenant isolation** — instances scoped to `tenant_id` from JWT; SuperAdmin may list all
4. **Audit** — `created_by`, `created_at`, `updated_at` on instances

---

## 2. Data model

### Entity relationship

```
procedure_types (existing)
procedure_instances
├── id (UUID PK)
├── tenant_id (indexed)
├── procedure_type_id (FK)
├── procedure_type_code (denormalized)
├── ot_tenant_id (tenant of managing OT)
├── ot_divipol_code
├── vehicle_query_value
├── status (PendienteEnvio)
├── created_by, created_at, updated_at

procedure_instance_actors
├── id, procedure_instance_id (FK, cascade)
├── role_label, sort_order
├── person_kind (Natural | Juridica)
├── document_type (Cc | Ce | Passport | Nit)
├── document_number
├── is_legal_representative (bool)
├── parent_actor_id (nullable FK self)
├── external_data_json (nullable JSONB)

procedure_instance_documents
├── id, procedure_instance_id (FK, cascade)
├── label, kind (Static)
├── storage_path, file_name, file_size_bytes
├── uploaded_at
```

### Legal representative rule

When `person_kind = Juridica` for a parametrized actor:

1. Capture company data (NIT fixed as document type, RUES stub lookup).
2. Require one **Representante Legal** child row: `person_kind = Natural`, `is_legal_representative = true`, `parent_actor_id` → juridical actor.
3. Representative uses RUNT (real) + SIMIT/RNMC stubs.

Not added to #9555 parametrization.

### Status enum

| Value | Meaning |
|-------|---------|
| `PendienteEnvio` | Created, awaiting future send-to-OT action |

---

## 3. API design

### `/api/v1/tramites`

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/index` | `tramites:read` | Paginated grid, `created_at DESC` |
| GET | `/procedure-types` | `tramites:create` | Active types from `IProcedureDefinitionService.ListActiveAsync` |
| GET | `/traffic-authorities` | `tramites:create` | Enabled company OTs (matrix ∩ active profile) |
| GET | `/lookups/rues` | `tramites:create` | Stub: `{ nit }` → fixed JSON |
| GET | `/lookups/simit` | `tramites:create` | Stub: `{ documentType, documentNumber }` |
| GET | `/lookups/rnmc` | `tramites:create` | Stub: `{ documentType, documentNumber }` |
| POST | `/` | `tramites:create` | Create instance (actors + metadata; documents via separate upload) |
| POST | `/{id:guid}/documents` | `tramites:create` | Multipart PDF upload per static document label |
| GET | `/{id:guid}` | `tramites:read` | Detail for grid row expansion (optional MVP) |

**Create request (POST `/`):**

```json
{
  "procedureTypeId": "uuid",
  "otDivipolCode": "11001000",
  "vehicleQueryValue": "ABC123",
  "actors": [
    {
      "roleLabel": "Vendedor",
      "sortOrder": 1,
      "personKind": "Natural",
      "documentType": "Cc",
      "documentNumber": "1234567890",
      "legalRepresentative": null
    },
    {
      "roleLabel": "Comprador",
      "sortOrder": 2,
      "personKind": "Juridica",
      "documentType": "Nit",
      "documentNumber": "900123456",
      "legalRepresentative": {
        "documentType": "Cc",
        "documentNumber": "9876543210"
      }
    }
  ]
}
```

**Traffic authorities response item:**

```json
{
  "divipolCode": "11001000",
  "displayName": "Secretaría Distrital de Movilidad - Bogotá",
  "otTenantId": "uuid"
}
```

**Validation:**

- `procedureTypeId` must be active
- `otDivipolCode` must be in enabled matrix and have active OT profile
- Actor count and `roleLabel`/`sortOrder` must match definition
- Juridical actors must include `legalRepresentative`
- Vehicle query uses existing `/api/v1/runt/{placa|vin}` from frontend before submit

**Document upload:**

- Content-Type `multipart/form-data`, field `file`
- Max size 10 MB (configurable)
- Reject non-PDF with `400`

---

## 4. Frontend design

### Routes

| Route | Purpose |
|-------|---------|
| `/tramites` | Index grid + “Nuevo trámite” button |

### Navigation

Add to main nav (not admin-only):

```ts
if (hasPermission(session, "tramites:read")) {
  items.push({ href: "/tramites", label: "Trámites" });
}
```

### Modal wizard (`ProcedureInstanceWizard`)

| Step | UI |
|------|-----|
| 1 | Select active procedure type (name + code) |
| 2 | Select OT from traffic authorities list |
| 3 | Input placa or VIN + “Consultar RUNT” |
| 4 | Per actor: person kind, doc type/number, consult buttons; jurídica → rep. legal sub-form |
| 5 | Per static document: PDF file input with validation |
| 6 | Summary + “Crear trámite” |

On success: close modal, refresh grid, toast “Trámite creado — pendiente de envío”.

### Grid columns

Reference (short id), Tipo, OT, Vehículo, Estado, Fecha, Creador.

---

## 5. External lookups (MVP A)

| Source | Actor | Implementation |
|--------|-------|----------------|
| RUNT | Vehicle, Natural actors, Legal rep | Existing `GET /api/v1/runt/{type}?q=` |
| RUES | Jurídica | Stub returns `{ razonSocial, estado }` |
| SIMIT | Natural, Legal rep | Stub returns `{ comparendos: [] }` |
| RNMC | Natural, Legal rep | Stub returns `{ medidas: [] }` |

Stubs live in `Flit.Procedures.Runtime/Lookups/`; interface `IExternalLookupService` for future swap.

---

## 6. #9558 alignment

| #9558 asset | MVP A usage |
|-------------|-------------|
| `ot_profiles` | OT picker (active + divipol match) |
| `traffic_authorities` + company matrix | Filter available OTs per company tenant |
| `IOtDocumentOrderService` | Not used in MVP A (future PDF merge order) |
| `IOtIntegrationModeService` | Not used in MVP A (future QX vs Dashboard) |
| RF01 unified menu | Resolved by `/tramites` nav in #9733 |

**Conclusion:** No new user stories on #9558. Optional post-MVP: shared `GET /api/v1/ot/catalog` if multiple consumers emerge.

---

## 7. Testing strategy

### Integration tests (`services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/`)

| Class | Coverage |
|-------|----------|
| `TramitesMigrationTests` | Instance tables exist |
| `TramitesIndexTests` | Pagination, tenant isolation, SuperAdmin global |
| `TramitesCreateTests` | Happy path, jurídica + rep legal, invalid OT, actor mismatch |
| `TramitesDocumentUploadTests` | PDF accepted, non-PDF rejected |
| `TramitesAuthTests` | Forbidden without `tramites:read`/`tramites:create` |

### E2E (`frontend/e2e/tramites/`)

| Spec | Flow |
|------|------|
| `tramites-index.spec.ts` | Grid visible for tenant admin |
| `tramites-create.spec.ts` | Full wizard create (mock/stub APIs) |

### Dev seed

Add `tramites:read` and `tramites:create` to `TenantA-Admin` in `DevTenantSeeder`.

---

## 8. Implementation phases (ADO mapping)

| Phase | HU | Deliverable | Branch |
|-------|-----|-------------|--------|
| HU-1 | #10071 | Schema, enums, migration | `feature/HU10071-DCHICA-schema-procedure-instances` |
| HU-2 | #10072 | Runtime API (index, create, OT list, stubs) | `feature/HU10072-DCHICA-api-tramites-runtime` |
| HU-3 | #10073 | PDF upload + local storage | `feature/HU10073-DCHICA-api-tramites-documents` |
| HU-4 | #10074 | FE grid + nav + permissions | `feature/HU10074-DCHICA-fe-tramites-index` |
| HU-5 | #10075 | FE modal wizard 6 steps | `feature/HU10075-DCHICA-fe-tramites-wizard` |

**Dependency order:** HU-1 → HU-2 → HU-3 → HU-4 → HU-5. Pause for review between HUs per `flit-hu-story-git-workflow`.

---

## 9. RF coverage (ADO #9733)

| RF | MVP A | Notes |
|----|-------|-------|
| RF01 | ❌ | Compound IDs — post-MVP |
| RF02 | Partial | Tenant-scoped grid; OT cross-tenant deferred |
| RF03 | ❌ | SuperMaestro commands — post-MVP |
| RF04 | ✅ | Dynamic stepper from parametrization |
| RF05 | Partial | RUNT real; others stubbed |
| RF06 | ❌ | Non-blocking banners — post-MVP |
| RF07 | ❌ | Copropiedad 100% — post-MVP |
| RF08 | ❌ | Hidden sellers accordion — post-MVP |
| RF09 | Partial | Static PDF upload; no async signatures |
