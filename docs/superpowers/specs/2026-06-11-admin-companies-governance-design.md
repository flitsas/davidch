# Admin Compañías — SaaS Multi-Tenant Governance Console

**Date:** 2026-06-11  
**Status:** Proposed  
**ADO Feature:** [#9551](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9551) — [ADMIN-COMPAÑÍAS] Consola de Gobierno SaaS Multi-Tenant  
**ADO User Stories:** [#9804](https://dev.azure.com/FlitDevOps/_workitems/edit/9804)–[#9815](https://dev.azure.com/FlitDevOps/_workitems/edit/9815)  
**Project:** FLIT 2.0 — FLIT EVOLUTION  
**Depends on:** [Identity JWT/RBAC design](./2026-06-09-identity-jwt-rbac-design.md) (Feature #9548, implemented)

## Summary

Design the **Admin Compañías** module: a SuperAdmin-only governance console and backend services to index B2B companies, configure multi-tab tenant policies (Matrícula, Traspasos, Config Empresa, Contingencia), and expose runtime policy evaluators plus a RUNT proxy with hot-failover.

This spec covers all 13 functional requirements (RF01–RF13) from Feature #9551, decomposed into 12 user stories (#9804–#9815).

## Locked decisions

| Area | Decision |
|------|----------|
| Module placement | New `Flit.Companies` module in existing .NET modular monolith (same deployable as Identity) |
| Company vs tenant | **Separate `companies` table** with `companies.tenant_id → tenants.id` (1:1 UNIQUE FK) |
| Config persistence | **Hybrid:** normalized columns for runtime flags; JSONB for provider credentials and rare fields |
| Provisioning | **Both modes:** atomic create (tenant + company + defaults) OR link existing tenant |
| Authorization | SuperAdmin only (`is_super_admin` JWT claim); no new RBAC permissions for MVP |
| RUNT MVP | **Real Verifik sandbox** + **Intempo stub** (configurable failure modes for failover tests) |
| Traffic authorities | **Global seed catalog** `traffic_authorities` + per-company enable matrix |
| Frontend | Next.js App Router under `/admin/companies/*` |
| API prefix | `/api/v1/admin/companies/*` (admin), `/api/v1/runt/*` (RUNT proxy) |
| Database | PostgreSQL shared schema; `CompaniesDbContext` with FK to `tenants` (read-only cross-reference) |
| Secrets encryption | **Deferred** — JSONB credentials stored plain in dev/MVP; encryption at rest in follow-up |

## Non-goals (MVP)

- B2B self-service or end-user company admin UI
- Hosting or developing external RUNT platforms
- Real Intempo production HTTP integration
- JSONB field-level encryption at rest
- Tramites module wiring (interfaces exposed; consumers in later feature)
- New RBAC permissions beyond SuperAdmin bypass

---

## 1. Architecture overview

### System diagram

```
Browser (SuperAdmin)
   │
   ▼
YARP Gateway
   ├── /api/v1/admin/companies/*  ──► Flit.Companies.Api
   ├── /api/v1/runt/*             ──► Flit.Companies.Runt
   ├── /api/auth|users|roles/*    ──► Flit.Identity (unchanged)
   └── /admin/companies/*         ──► Next.js 16
                                          │
                                          ▼
                                     PostgreSQL
```

### Module layout

| Project | Responsibility |
|---------|----------------|
| `Flit.Companies.Api` | Minimal APIs, DTOs, FluentValidation, SuperAdmin guard |
| `Flit.Companies.Core` | Domain services, runtime policy evaluators, RUNT Strategy |
| `Flit.Companies.Infrastructure` | EF Core entities, migrations, adapters, seeders |
| `Flit.Companies.Shared` | Enums, error codes, public interfaces for Tramites module |

### Runtime policy interfaces (for future Tramites)

```csharp
IMatriculaPolicy.Evaluate(tenantId, procedureType)
ITraspasoPolicy.Evaluate(tenantId, userId, vehicleOwnerNit)
ISignatureOrchestrator.Resolve(tenantId, role: Seller|Buyer)
INotificationRouter.ResolveChannel(tenantId) / ResolveTarget(tenantId)
IPaymentPolicy.GetAuthorizedMethods(tenantId)
ITrafficAuthorityPolicy.IsEnabled(tenantId, authorityCode)
IRuntQueryService.Query(tenantId, type, value)
```

Implemented in `Flit.Companies.Core`; registered in DI; called in-process (no HTTP hop).

### Cross-cutting concerns

1. **Authentication** — reuse Identity JWT cookie middleware (`flit_access`)
2. **Authorization** — all admin/RUNT routes require `is_super_admin == true`; return `403` with `ApiErrorCodes.Forbidden`
3. **Audit** — config mutations set `updated_at` + `updated_by`; SuperAdmin actions logged via existing `AuditService` pattern
4. **Tenant sync** — suspend/activate company mirrors `tenants.is_active` in same transaction

---

## 2. Data model

### Entity relationship

```
tenants (Identity)              companies
├── id ◄──────────────────────── tenant_id (FK, UNIQUE)
├── name                          ├── id (PK)
├── slug                          ├── nit (UNIQUE, indexed)
├── is_active                     ├── legal_name
└── ...                           ├── status (Active | Suspended)
                                  ├── created_at
                                  └── updated_at
```

**Invariants:**

- Every `companies` row references exactly one `tenants` row (1:1).
- A tenant may exist without a company (dev/internal tenants).
- NIT is unique platform-wide.
- On company save, optionally sync `tenants.name ← companies.legal_name`.

### `companies`

| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID PK | |
| `tenant_id` | UUID FK UNIQUE | → `tenants.id` |
| `nit` | VARCHAR UNIQUE | Indexed for grid filter |
| `legal_name` | VARCHAR | Indexed for grid filter |
| `status` | ENUM | `Active`, `Suspended` |
| `created_at` | TIMESTAMPTZ | |
| `updated_at` | TIMESTAMPTZ | Audit filter range |

### Config tables (one row per company unless noted)

#### `company_matricula_config` (RF03)

| Column | Type |
|--------|------|
| `company_id` | UUID PK/FK |
| `allow_new_vehicle_filing` | BOOLEAN |
| `allow_misc_procedures` | BOOLEAN |
| `updated_at` | TIMESTAMPTZ |
| `updated_by` | UUID FK → users |

#### `company_traspaso_config` (RF04)

| Column | Type |
|--------|------|
| `company_id` | UUID PK/FK |
| `only_own_vehicles` | BOOLEAN |
| `updated_at` | TIMESTAMPTZ |
| `updated_by` | UUID |

#### `tenant_user_exceptions` (RF05)

| Column | Type |
|--------|------|
| `tenant_id` | UUID FK |
| `user_id` | UUID FK |
| `created_at` | TIMESTAMPTZ |
| `created_by` | UUID |

Composite PK: `(tenant_id, user_id)`. Batch POST/DELETE endpoints.

#### `company_signature_config` (RF06–07)

| Column | Type |
|--------|------|
| `company_id` | UUID PK/FK |
| `seller_signature_type` | ENUM | `DigitalId`, `OnScreen`, `Preassigned` |
| `buyer_signature_type` | ENUM | same |
| `vault_enabled` | BOOLEAN |
| `vault_settings` | JSONB NULL | endpoint config when vault on |
| `updated_at` | TIMESTAMPTZ |
| `updated_by` | UUID |

#### `company_notification_config` (RF08–09)

| Column | Type |
|--------|------|
| `company_id` | UUID PK/FK |
| `channel` | ENUM | `FlitSmtp`, `ClientApi` |
| `notification_target` | ENUM | `Buyer`, `Filer`, `None` |
| `client_api_settings` | JSONB NULL | Renting gateway URL, headers |
| `updated_at` | TIMESTAMPTZ |
| `updated_by` | UUID |

#### `company_payment_config` (RF10)

| Column | Type |
|--------|------|
| `company_id` | UUID PK/FK |
| `allow_flit_gateway` | BOOLEAN |
| `allow_ot` | BOOLEAN |
| `allow_other` | BOOLEAN |
| `updated_at` | TIMESTAMPTZ |
| `updated_by` | UUID |

#### `company_runt_config` (RF11–12)

| Column | Type |
|--------|------|
| `company_id` | UUID PK/FK |
| `primary_provider` | ENUM | `Verifik`, `Intempo` |
| `secondary_provider` | ENUM | |
| `failover_timeout_ms` | INT | Default `4000` |
| `provider_credentials` | JSONB | Verifik API keys/endpoints |
| `updated_at` | TIMESTAMPTZ |
| `updated_by` | UUID |

#### `traffic_authorities` (global catalog seed)

| Column | Type |
|--------|------|
| `code` | VARCHAR PK | e.g. `11001000` |
| `name` | VARCHAR | |
| `region` | VARCHAR NULL | |
| `is_active` | BOOLEAN | Global catalog visibility |

#### `company_traffic_authority_matrix` (RF13)

| Column | Type |
|--------|------|
| `company_id` | UUID FK |
| `authority_code` | VARCHAR FK → traffic_authorities |
| `is_enabled` | BOOLEAN |

Composite PK: `(company_id, authority_code)`.

### Default seed on company create

All boolean switches `false`; `primary_provider=Verifik`, `secondary_provider=Intempo`; `failover_timeout_ms=4000`; all matrix rows `is_enabled=false`; notification `channel=FlitSmtp`, `target=Filer`.

### Indexes

- `companies(nit)`, `companies(legal_name)`, `companies(updated_at)`
- `tenant_user_exceptions(tenant_id)`
- `company_traffic_authority_matrix(company_id, is_enabled)`

---

## 3. Company provisioning

### Wizard step 1 — mode selection

| Mode | Use case |
|------|----------|
| `create` | New B2B client — provisions tenant atomically |
| `link` | Existing Identity tenant without company profile |

### `POST /api/v1/admin/companies`

**Create mode:**

```json
{
  "mode": "create",
  "nit": "900123456-1",
  "legal_name": "Renting Colombia SAS",
  "slug": "renting-co",
  "status": "Active"
}
```

Transaction: insert `tenants` → insert `companies` → seed all config rows → seed matrix rows for all catalog authorities (disabled).

**Link mode:**

```json
{
  "mode": "link",
  "tenant_id": "uuid",
  "nit": "900123456-1",
  "legal_name": "Renting Colombia SAS",
  "status": "Active"
}
```

Validations: tenant exists, active, no existing company; NIT unique.

### Suspend / activate

`PATCH /api/v1/admin/companies/{id}/status` with `{ "status": "Suspended" | "Active" }`:

- Updates `companies.status`
- Sets `tenants.is_active = (status == Active)`
- Blocks login and runtime trámite evaluation for suspended tenants

---

## 4. RUNT proxy (Strategy + hot-failover)

### Pattern

```
IRuntProvider (Strategy)
  ├── VerifikAdapter      — real HTTP, sandbox credentials from JSONB/env
  └── IntempoStubAdapter  — canned responses; dev/QA failure injection

RuntProxy : IRuntQueryService
  ├── Execute primary with CancellationTokenSource(failover_timeout_ms)
  ├── On TimeoutException or HTTP 5xx → execute secondary
  └── Log: tenant_id, query_type, used_provider, latency_ms, failover_reason
```

### Intempo stub modes (`appsettings` or `X-Runt-Stub-Mode` header)

| Mode | Behavior |
|------|----------|
| `success` | Return canned Placa/Conductor/VIN payload |
| `timeout` | Delay 5s (triggers failover) |
| `5xx` | Return 503 (triggers failover) |

### Query types

`Placa`, `Conductor`, `Vin` — enum mapped to provider-specific request shapes inside each adapter.

### MVP scope

- Verifik: real sandbox integration (CI skips if `VERIFIK_API_KEY` unset)
- Intempo: stub only; production adapter deferred

---

## 5. API surface

All admin routes require SuperAdmin. Pagination follows `{ items, totalCount, page, pageSize }`.

### Company CRUD & index (RF01–02) — #9805, #9806

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/admin/companies/index` | Paginated index; query: `page`, `pageSize`, `sort`, `nit`, `name`, `id`, `auditFrom`, `auditTo` |
| POST | `/api/v1/admin/companies` | Create (`mode: create\|link`) |
| GET | `/api/v1/admin/companies/{id}` | Detail + tenant summary |
| PATCH | `/api/v1/admin/companies/{id}` | Update NIT, legal_name |
| PATCH | `/api/v1/admin/companies/{id}/status` | Suspend / activate |

### Config tabs — #9807–9812

| Method | Path | RF |
|--------|------|-----|
| GET/PUT | `/api/v1/admin/companies/{id}/config/matricula` | RF03 |
| GET/PUT | `/api/v1/admin/companies/{id}/config/traspasos` | RF04 |
| POST/DELETE | `/api/v1/admin/companies/{id}/exceptions/batch` | RF05 |
| GET/PUT | `/api/v1/admin/companies/{id}/config/signatures` | RF06–07 |
| GET/PUT | `/api/v1/admin/companies/{id}/config/notifications` | RF08–09 |
| GET/PUT | `/api/v1/admin/companies/{id}/config/payments` | RF10 |
| GET/PUT | `/api/v1/admin/companies/{id}/config/runt` | RF11–12 |
| GET/PATCH | `/api/v1/admin/companies/{id}/traffic-authorities` | RF13 |

`PATCH traffic-authorities` accepts `{ "updates": [{ "authority_code", "is_enabled" }] }` for bulk toggle.

### RUNT runtime — #9811

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/runt/{placa\|conductor\|vin}?q={value}` | Resolve tenant from JWT; execute proxy |

### Error responses

Reuse Identity `ApiErrorCodes`: `Forbidden`, `NotFound`, `ValidationError`, `Conflict` (duplicate NIT, tenant already linked).

---

## 6. Runtime evaluation (RF03–05, RF09–10, RF13)

Called by future Tramites module via injected policies:

| RF | Evaluator | Rule |
|----|-----------|------|
| RF03 | `IMatriculaPolicy` | Deny if switch off for procedure type |
| RF04 | `ITraspasoPolicy` | If `only_own_vehicles` and vehicle NIT ≠ company NIT → deny |
| RF05 | `ITraspasoPolicy` | Unless `user_id` in `tenant_user_exceptions` |
| RF09 | `INotificationRouter` | Route to Buyer, Filer, or skip per `notification_target` |
| RF10 | `IPaymentPolicy` | Allow only checked payment methods |
| RF13 | `ITrafficAuthorityPolicy` | Deny if authority disabled in matrix |

Config loaded by `tenant_id` → `companies` → config row (cached per request via `IMemoryCache` with 60s TTL).

---

## 7. Frontend (Next.js)

### Routes — #9806, #9813–#9815

| Path | Story | Purpose |
|------|-------|---------|
| `/admin/companies` | #9806 | Index grid with filters |
| `/admin/companies/new` | #9813 | Wizard: create vs link |
| `/admin/companies/[id]?tab=...` | #9813–#9815 | Tabbed editor |

### Tabs

| Tab query | Content | Story |
|-----------|---------|-------|
| `matricula` | Matrícula switches | #9814 |
| `traspasos` | only_own_vehicles + whitelist batch UI | #9814 |
| `config-empresa` | Signature matrix + vault toggle | #9814 |
| `contingencia` | RUNT providers + traffic authority grid | #9815 |

Plus notification/payment fields in `contingencia` or sub-sections per UI mockup during implementation.

### Guards

- Middleware: `/admin/companies/*` requires authenticated SuperAdmin (check `/api/auth/me` → `is_super_admin`)
- Non-SuperAdmin → redirect `/login` or 403 page
- Server components use `apiFetch` with cookies (same as `/admin/users`)

### UI standards

- Poppins typography, existing FLIT component library
- Server-side pagination for index and traffic authority grid
- Independent save per tab (`PUT` + toast confirmation)

---

## 8. User story mapping

| ID | Title | Phase | SP | RFs |
|----|-------|-------|-----|-----|
| #9804 | Schema PostgreSQL + EF Core | 1 | 5 | base |
| #9805 | API indexación + SuperAdmin | 1 | 5 | RF01–02 |
| #9806 | Consola indexación B2B | 1 | 8 | RF01–02 |
| #9807 | API Matrícula Inicial | 2 | 3 | RF03 |
| #9808 | API Traspasos + lista blanca | 2 | 5 | RF04–05 |
| #9813 | Shell multi-pestaña + CRUD | 2 | 5 | RF01 |
| #9814 | UI Matrícula / Traspasos / Firmas | 2–3 | 8 | RF03–07 |
| #9809 | API matriz firmas + baúl | 3 | 5 | RF06–07 |
| #9810 | API notificaciones + recaudo | 3 | 5 | RF08–10 |
| #9815 | UI Notificaciones / Contingencia | 3–4 | 8 | RF08–10, 13 |
| #9811 | Proxy RUNT + failover | 4 | 8 | RF11–12 |
| #9812 | API organismos de tránsito | 4 | 3 | RF13 |

**Total:** 68 story points.

**Dependency chain:** `#9804 → #9805 → #9806 → #9813 → backends (#9807–#9812) → frontends (#9814–#9815)`.

---

## 9. Testing strategy

### Integration tests (Testcontainers PostgreSQL)

- Company create both modes; 1:1 tenant constraint enforced
- Duplicate NIT rejected; link to tenant with existing company rejected
- Suspend mirrors `tenants.is_active`
- Config PUT/GET round-trip per tab
- Whitelist batch insert/delete
- RUNT failover: Intempo stub `timeout`/`5xx` → Verifik used

### RUNT sandbox

- Verifik adapter tests marked `[Trait("Category", "RuntSandbox")]`, skipped in CI without credentials

### E2E (Playwright)

- SuperAdmin login → index → create company (atomic) → edit Matrícula tab → save

### Postman

- Extend or add `FLIT-Admin-Companies-API` collection mirroring Identity collection pattern

---

## 10. Implementation phases

| Phase | Stories | Deliverable |
|-------|---------|-------------|
| **1 — Foundation** | #9804, #9805, #9806 | Schema, index API, admin grid |
| **2 — Config core** | #9807, #9808, #9813, #9814 | Tab shell + Matrícula/Traspasos/Firmas |
| **3 — Runtime policies** | #9809, #9810, #9815 | Notifications, payments, UI completion |
| **4 — Integrations** | #9811, #9812 | RUNT proxy + traffic authority matrix |

---

## 11. Open items (post-MVP)

| Item | Notes |
|------|-------|
| JSONB encryption at rest | Use ASP.NET Data Protection or DB-level encryption |
| Real Intempo adapter | Replace stub when vendor contract ready |
| Tramites module integration | Inject `I*Policy` services |
| `companies:read` RBAC permission | If non-SuperAdmin viewers needed later |
| Audit log per-field diff | MVP logs row-level `updated_by` only |
