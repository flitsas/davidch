# Dashboard Trámites Operativos — MVP A

**Date:** 2026-06-12  
**Status:** Approved  
**ADO Feature:** [#9723](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9723) — [DASHBOARD-TRÁMITES] Dashboard de Trámites Operativos  
**ADO User Stories:** TBD (HU-1–HU-3 below)  
**Project:** FLIT 2.0 — FLIT EVOLUTION  
**Depends on:** Identity (#9548), Admin Compañías (#9551), Trámites Runtime (#9733)

## Summary

Read-only analytics dashboard for **Tenant Admin** and **SuperAdmin** over procedure instances created in #9733. Shows date-filtered pie charts (Matrículas / Traspasos / Otros), a lateral detail table on segment click, Top 5 radicator user cards with multiselect customization, and simple Excel export. Queries run directly against PostgreSQL in real time — no snapshot tables.

MVP A intentionally omits approval dates, chunked Excel, and executive PDF (deferred to a later phase when procedure lifecycle exists).

## Locked decisions

| Area | Decision |
|------|----------|
| MVP scope | Minimal post-#9733 MVP A — radicated procedures only; no `approved_at` |
| Module placement | New handlers in existing `Flit.Procedures.Runtime` (not a separate Analytics project) |
| Database | No new tables; query `procedure_instances` + `procedure_instance_actors` |
| API prefix | `/api/v1/tramites/dashboard/*` |
| Frontend route | `/tramites/dashboard` |
| Auth | `users:read` (Tenant Admin) or SuperAdmin Global — operators without `users:read` cannot access |
| Tenant isolation | Tenant Admin: JWT `tenant_id`; SuperAdmin: optional `?tenantId=` filter |
| Date filter default | Last 30 days (`from` / `to` ISO date query params) |
| Chart categories | Code mapping: `MATRICULA*` → Matrículas, `TRASPASO` → Traspasos, else → Otros |
| Owner name | Heuristic on `role_label` (propietario, comprador, titular), fallback `sort_order = 1`; parse `external_data_json` for name; fallback `"{DocumentType} {DocumentNumber}"` |
| Excel export | Single-request `.xlsx` via ClosedXML; max 10 000 rows; no chunk streaming |
| PDF executive summary | Deferred |
| Chart library | `recharts` in frontend |

## Non-goals (MVP A)

- Create, edit, or mutate procedures from the dashboard
- `approved_at` column or post-`PendienteEnvio` workflow data
- Chunked/streaming Excel for very high volume
- Executive PDF with embedded charts
- Scheduled email reports or alerts
- Precomputed aggregation tables / materialized views
- New parametrizador field for chart category

---

## 1. Architecture

```
Browser (Tenant Admin | SuperAdmin)
   │
   ▼
YARP Gateway
   ├── /api/v1/tramites/dashboard/*  → Flit.Procedures.Runtime/Dashboard/
   └── /tramites/dashboard/*         → Next.js (@flit/frontend)
                                          │
                                          ▼
                                     PostgreSQL (procedure_instances, procedure_instance_actors)
                                     Identity DB (users for radicator names)
```

### Module layout

| Path | Responsibility |
|------|----------------|
| `Flit.Procedures.Runtime/Dashboard/TramitesDashboardSummaryHandler.cs` | Category counts |
| `Flit.Procedures.Runtime/Dashboard/TramitesDashboardDetailHandler.cs` | Paginated detail rows |
| `Flit.Procedures.Runtime/Dashboard/TramitesDashboardUsersHandler.cs` | Top 5 + search + per-user stats |
| `Flit.Procedures.Runtime/Dashboard/TramitesDashboardExportHandler.cs` | Excel generation |
| `Flit.Procedures.Runtime/Dashboard/OwnerNameResolver.cs` | Actor → display name |
| `Flit.Procedures.Runtime/Dashboard/ProcedureCategoryClassifier.cs` | Code → Matrículas/Traspasos/Otros |
| `Flit.Procedures.Runtime/Auth/RequireDashboardAccessExtensions.cs` | `users:read` filter |
| `Flit.Procedures.Runtime/Endpoints/TramitesEndpoints.cs` | Map dashboard routes |

Registered in `Flit.Identity.Api/Program.cs` via existing `MapTramitesEndpoints`.

### Cross-cutting

1. **Authentication** — JWT cookie (`flit_access`) via existing middleware
2. **Authorization** — `RequireDashboardAccess` → `users:read` (Tenant scope) or SuperAdmin bypass
3. **Tenant isolation** — filter `procedure_instances.tenant_id`; SuperAdmin may omit filter or pass `tenantId`
4. **Real-time** — all endpoints query live data; no cache layer in MVP A

### Category classification

```csharp
static string Classify(string procedureTypeCode) =>
    procedureTypeCode.StartsWith("MATRICULA", StringComparison.OrdinalIgnoreCase) ? "matriculas"
    : string.Equals(procedureTypeCode, "TRASPASO", StringComparison.OrdinalIgnoreCase) ? "traspasos"
    : "otros";
```

---

## 2. API design

Base: `/api/v1/tramites/dashboard` — all routes require dashboard access.

### Common query parameters

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `from` | ISO date | No | Default: today − 30 days (UTC, start of day) |
| `to` | ISO date | No | Default: today (UTC, end of day) |
| `tenantId` | UUID | No | SuperAdmin only; scopes all queries |

Validation: `from` ≤ `to`; range max 366 days → `400 VALIDATION_ERROR`.

### Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/summary` | Counts per category + `total` |
| GET | `/detail` | Paginated rows for one category |
| GET | `/users/top` | Top 5 users by procedure count in range |
| GET | `/users` | Searchable user list for multiselect (`q`, `page`, `pageSize`) |
| GET | `/users/{userId}/stats` | Single user count in range |
| GET | `/export` | Download `.xlsx` for category + date range |

### GET `/summary`

**Response:**

```json
{
  "from": "2026-05-13",
  "to": "2026-06-12",
  "total": 42,
  "categories": [
    { "key": "matriculas", "label": "Matrículas", "count": 18, "percent": 42.9 },
    { "key": "traspasos", "label": "Traspasos", "count": 20, "percent": 47.6 },
    { "key": "otros", "label": "Otros trámites", "count": 4, "percent": 9.5 }
  ]
}
```

Filter: `created_at` between `from` 00:00 UTC and `to` 23:59:59 UTC.

### GET `/detail`

| Param | Type | Required |
|-------|------|----------|
| `category` | `matriculas\|traspasos\|otros` | Yes |
| `page` | int | No (default 1) |
| `pageSize` | int | No (default 20, max 100) |

**Response row:**

```json
{
  "id": "A1B2C3D4",
  "procedureInstanceId": "uuid",
  "radicatedAt": "2026-06-01T14:30:00Z",
  "status": "PendienteEnvio",
  "plate": "ABC123",
  "ownerName": "María García",
  "updatedAt": "2026-06-01T14:30:00Z"
}
```

`id` = first 8 chars of UUID uppercased (consistent with `/tramites/index`).

### GET `/users/top`

Returns up to 5 users ordered by `COUNT(procedure_instances)` where `created_by = user.id` in date range.

```json
{
  "items": [
    { "userId": "uuid", "displayName": "Ana López", "email": "ana@tenant-a.com", "count": 12 }
  ]
}
```

Join `IdentityDbContext.Users` for `displayName` / `email` (email only if no display name).

### GET `/users`

Search tenant users for multiselect. Params: `q` (optional, matches email), `page`, `pageSize` (max 50). Excludes SuperAdmin system users outside tenant.

### GET `/users/{userId}/stats`

```json
{ "userId": "uuid", "count": 0 }
```

Frontend shows card message when `count === 0`: *"Este usuario no ha radicado ningún trámite"*.

### GET `/export`

Same filters as `/detail` (`category`, `from`, `to`, `tenantId`). Returns file stream:

- Content-Type: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- Filename: `tramites-{category}-{from}-{to}.xlsx`
- Columns: ID, Fecha radicación, Estado, Placa, Nombre propietario, Fecha actualización
- Cap: 10 000 rows; if exceeded → `400 EXPORT_LIMIT_EXCEEDED` with message to narrow date range

### Owner name resolution (`OwnerNameResolver`)

1. Select actor: first non–legal-representative actor whose `role_label` matches `(?i)(propietario|comprador|titular)`; else actor with lowest `sort_order` (excluding `IsLegalRepresentative`).
2. If `PersonKind = Juridica`: read `razonSocial` from `external_data_json` (RUES shape).
3. If `PersonKind = Natural`: read `nombre` from `external_data_json` (RUNT shape).
4. Fallback: `"{DocumentType} {DocumentNumber}"`.

Note: `external_data_json` may be null until #9733 wires lookup results on create; fallback is acceptable in MVP A.

---

## 3. Frontend design

### Routes

| Route | Purpose |
|-------|---------|
| `/tramites/dashboard` | Analytics dashboard |

### Layout guard

Extend or sibling to `/tramites/layout.tsx`:

- Allow if `hasPermission(session, "users:read")` OR `session.isSuperAdmin`
- Deny operators with only `tramites:create`

### Navigation

```ts
if (hasPermission(session, "users:read") || session.isSuperAdmin) {
  items.push({ href: "/tramites/dashboard", label: "Dashboard" });
}
```

Place after "Trámites" in `buildAdminNav`.

### Page structure

```
┌─────────────────────────────────────────────────────────────┐
│ PageHeaderCard: Dashboard de Trámites                       │
│ [Date from] [Date to]  [Tenant select if SuperAdmin]        │
│ [Exportar Excel] (enabled when detail panel has category)   │
├──────────────────┬──────────────────────────────────────────┤
│ DetailTablePanel │ DonutChart (recharts)                      │
│ (left, visible   │ Legend: Matrículas / Traspasos / Otros     │
│  on segment      │                                            │
│  click)          │ UserProductivityCards (Top 5 default)      │
│                  │ UserMultiselect + dynamic cards            │
└──────────────────┴──────────────────────────────────────────┘
```

Mobile: stack vertically — chart first, detail table below on segment click.

### Components

| File | Responsibility |
|------|----------------|
| `app/tramites/dashboard/page.tsx` | Server shell + initial summary fetch |
| `app/tramites/dashboard/layout.tsx` | Dashboard auth guard (optional if shared with tramites layout) |
| `components/tramites/dashboard/TramitesDashboard.tsx` | Client orchestrator |
| `components/tramites/dashboard/DateRangeFilter.tsx` | From/to inputs |
| `components/tramites/dashboard/CategoryDonutChart.tsx` | recharts PieChart |
| `components/tramites/dashboard/DetailTablePanel.tsx` | Lateral table |
| `components/tramites/dashboard/UserProductivityCard.tsx` | Single user card |
| `components/tramites/dashboard/UserMultiselect.tsx` | Search + add cards |
| `components/tramites/dashboard/TenantSelector.tsx` | SuperAdmin only |
| `lib/tramites/dashboard-api.ts` | API helpers |
| `lib/tramites/dashboard-types.ts` | DTO types |

### Interaction flow

1. Page loads → fetch `/summary` + `/users/top` with default 30-day range
2. User changes dates → refetch summary, top users, clear selected category
3. User clicks pie segment → fetch `/detail?category=…` → show left panel
4. User clicks Export → `window.location` or blob download from `/export?category=…`
5. User searches/adds users in multiselect → fetch `/users/{id}/stats` per card

### Dependencies

Add to `frontend/package.json`:

- `recharts` — donut chart

---

## 4. Testing strategy

### Integration tests (`services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/Dashboard/`)

| Test | Assert |
|------|--------|
| `TramitesDashboardAuthTests` | 403 without `users:read`; 200 for Tenant Admin |
| `TramitesDashboardSummaryTests` | Correct category counts for seeded instances |
| `TramitesDashboardDetailTests` | Category filter + pagination; tenant isolation |
| `TramitesDashboardUsersTests` | Top 5 ordering; zero-count user |
| `TramitesDashboardExportTests` | Returns valid xlsx content-type; respects 10k cap |

Seed: create instances with `MATRICULA_INICIAL`, `TRASPASO`, `RADICADO_CUENTA` codes across two users.

### E2E (`frontend/e2e/tramites/tramites-dashboard.spec.ts`)

1. Login as Tenant Admin → `/tramites/dashboard` renders chart
2. Change date range → summary updates
3. Click segment → detail table visible with expected columns

---

## 5. RF coverage (ADO #9723)

| RF | MVP A |
|----|-------|
| RF01 — Access by role | ✅ `users:read` + SuperAdmin |
| RF02 — Date range filter | ✅ Real-time DB query |
| RF03 — Pie charts | ✅ 3 categories |
| RF04 — Lateral detail table | ✅ No modals |
| RF05 — Mandatory columns | ⚠️ No `fecha aprobación` |
| RF06 — Excel export | ⚠️ Simple export, not chunked |
| RF07 — Top 5 users | ✅ |
| RF08 — User multiselect | ✅ |
| RF09 — Zero-tramite card message | ✅ |
| RF10 — Executive PDF | ❌ Deferred |

---

## 6. User stories (proposed for ADO)

| HU | Title | Scope |
|----|-------|-------|
| HU-1 | API dashboard trámites | Summary, detail, users, export endpoints + integration tests |
| HU-2 | FE dashboard trámites | Page, chart, lateral table, cards, multiselect, Excel download |
| HU-3 | E2E dashboard trámites | Playwright happy path |

**Dependency order:** #9733 HU-1 (schema) + HU-2 (create API) must be merged before HU-1 here. HU-2 (FE dashboard) depends on HU-1 (API dashboard).

**Suggested branches:**

- `feature/HU{ID}-DCHICA-api-tramites-dashboard`
- `feature/HU{ID}-DCHICA-fe-tramites-dashboard`
- `feature/HU{ID}-DCHICA-e2e-tramites-dashboard`

---

## 7. Future phases (out of scope)

- `approved_at` column when send-to-OT workflow ships
- Chunked Excel for >10k rows
- Executive PDF (server-side chart render or client snapshot)
- Dedicated `tramites:analytics:read` permission if RBAC needs finer control
- `Flit.Procedures.Analytics` module split if query complexity grows
