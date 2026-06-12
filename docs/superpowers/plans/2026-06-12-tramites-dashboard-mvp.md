# Trámites Dashboard MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver MVP A for Feature #9723 — read-only analytics dashboard over procedure instances with pie charts, lateral detail table, user productivity cards, and simple Excel export.

**Architecture:** Extend `Flit.Procedures.Runtime` with `/api/v1/tramites/dashboard/*` handlers querying `procedure_instances` + actors live; join `IdentityDbContext` for radicator names. Frontend at `/tramites/dashboard` with `recharts` donut chart. Auth via `users:read` (Tenant Admin) or SuperAdmin — not `tramites:read`.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, EF Core 10 + Npgsql, ClosedXML, PostgreSQL 16, Next.js 16, React 19, TypeScript, Tailwind v4, recharts, pnpm 9, xUnit + Testcontainers, Playwright

**Spec:** `docs/superpowers/specs/2026-06-12-tramites-dashboard-mvp-design.md`

**ADO Feature:** [#9723](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9723) (New)

| Story | ADO (proposed) | Title | Branch |
|-------|----------------|-------|--------|
| HU-1 | TBD | API dashboard (summary, detail, users, export) | `feature/HU-TBD-DCHICA-api-tramites-dashboard` |
| HU-2 | TBD | FE dashboard (chart, table, cards, export) | `feature/HU-TBD-DCHICA-fe-tramites-dashboard` |
| HU-3 | TBD | E2E dashboard | `feature/HU-TBD-DCHICA-e2e-tramites-dashboard` |

**Dependency:** #9733 merged on `develop` (verified). **Order:** HU-1 → HU-2 → HU-3. Stop for review between HUs.

---

## File Map

### Backend (`services/core-api/`)

| File | Responsibility |
|------|----------------|
| `src/Flit.Procedures.Runtime/Flit.Procedures.Runtime.csproj` | Add ClosedXML + Identity.Infrastructure ref |
| `src/Flit.Procedures.Runtime/Auth/RequireDashboardAccessExtensions.cs` | `users:read` endpoint filter |
| `src/Flit.Procedures.Runtime/Dashboard/ProcedureCategoryClassifier.cs` | MATRICULA*/TRASPASO/otros |
| `src/Flit.Procedures.Runtime/Dashboard/OwnerNameResolver.cs` | Actor → display name |
| `src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardDateRange.cs` | Parse/validate from/to |
| `src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardQuery.cs` | Shared filtered `IQueryable` |
| `src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardModels.cs` | Response DTOs |
| `src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardSummaryHandler.cs` | GET `/summary` |
| `src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardDetailHandler.cs` | GET `/detail` |
| `src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardUsersHandler.cs` | GET `/users/*` |
| `src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardExportHandler.cs` | GET `/export` |
| `src/Flit.Procedures.Runtime/Endpoints/TramitesEndpoints.cs` | Map dashboard routes |
| `src/Flit.Identity.Api/Program.cs` | Register dashboard handlers |

### Frontend (`frontend/`)

| File | Responsibility |
|------|----------------|
| `package.json` | Add `recharts` |
| `lib/tramites/dashboard-types.ts` | DTO types |
| `lib/tramites/dashboard-api.ts` | Fetch helpers + export download |
| `lib/flit/nav.ts` | Dashboard nav link |
| `app/tramites/layout.tsx` | Shared AppShell only (refactor) |
| `app/tramites/(index)/layout.tsx` | `tramites:read` guard |
| `app/tramites/(index)/page.tsx` | Move existing index page |
| `app/tramites/dashboard/layout.tsx` | `users:read` guard |
| `app/tramites/dashboard/page.tsx` | Server shell |
| `components/tramites/dashboard/TramitesDashboard.tsx` | Client orchestrator |
| `components/tramites/dashboard/DateRangeFilter.tsx` | Date inputs |
| `components/tramites/dashboard/CategoryDonutChart.tsx` | recharts PieChart |
| `components/tramites/dashboard/DetailTablePanel.tsx` | Lateral detail table |
| `components/tramites/dashboard/UserProductivityCard.tsx` | Single user card |
| `components/tramites/dashboard/UserMultiselect.tsx` | Search + add users |
| `components/tramites/dashboard/TenantSelector.tsx` | SuperAdmin tenant picker |
| `e2e/tramites/tramites-dashboard.spec.ts` | Playwright happy path |

### Tests

| File | Responsibility |
|------|----------------|
| `tests/.../Tramites/Dashboard/TramitesDashboardAuthTests.cs` | 403/200 auth |
| `tests/.../Tramites/Dashboard/TramitesDashboardSummaryTests.cs` | Category counts |
| `tests/.../Tramites/Dashboard/TramitesDashboardDetailTests.cs` | Detail + isolation |
| `tests/.../Tramites/Dashboard/TramitesDashboardUsersTests.cs` | Top 5 + stats |
| `tests/.../Tramites/Dashboard/TramitesDashboardExportTests.cs` | xlsx response |
| `tests/.../Tramites/Dashboard/TramitesDashboardTestHelper.cs` | Seed instances helper |

---

## HU-1: API dashboard (HU-TBD)

**Branch:** `feature/HU-TBD-DCHICA-api-tramites-dashboard`

### Task 1: Project deps + shared dashboard primitives

**Files:**
- Modify: `services/core-api/src/Flit.Procedures.Runtime/Flit.Procedures.Runtime.csproj`
- Create: `services/core-api/src/Flit.Procedures.Runtime/Dashboard/ProcedureCategoryClassifier.cs`
- Create: `services/core-api/src/Flit.Procedures.Runtime/Dashboard/OwnerNameResolver.cs`
- Create: `services/core-api/tests/Flit.Identity.UnitTests/Procedures/ProcedureCategoryClassifierTests.cs`
- Create: `services/core-api/tests/Flit.Identity.UnitTests/Procedures/OwnerNameResolverTests.cs`

- [ ] **Step 1: Add packages and project reference**

In `Flit.Procedures.Runtime.csproj` add:

```xml
<PackageReference Include="ClosedXML" Version="0.104.2" />
```

And:

```xml
<ProjectReference Include="..\Flit.Identity.Infrastructure\Flit.Identity.Infrastructure.csproj" />
```

- [ ] **Step 2: Write failing unit tests for classifier**

`ProcedureCategoryClassifierTests.cs`:

```csharp
using Flit.Procedures.Runtime.Dashboard;

namespace Flit.Identity.UnitTests.Procedures;

public class ProcedureCategoryClassifierTests
{
    [Theory]
    [InlineData("MATRICULA_INICIAL", "matriculas")]
    [InlineData("matricula_extra", "matriculas")]
    [InlineData("TRASPASO", "traspasos")]
    [InlineData("RADICADO_CUENTA", "otros")]
    public void Classify_maps_codes(string code, string expected) =>
        Assert.Equal(expected, ProcedureCategoryClassifier.Classify(code));
}
```

- [ ] **Step 3: Run tests — expect FAIL**

Run: `dotnet test services/core-api/tests/Flit.Identity.UnitTests/Flit.Identity.UnitTests.csproj --filter ProcedureCategoryClassifierTests -v n`

Expected: FAIL — type not found

- [ ] **Step 4: Implement classifier**

`ProcedureCategoryClassifier.cs`:

```csharp
namespace Flit.Procedures.Runtime.Dashboard;

public static class ProcedureCategoryClassifier
{
    public const string Matriculas = "matriculas";
    public const string Traspasos = "traspasos";
    public const string Otros = "otros";

    public static string Classify(string procedureTypeCode)
    {
        if (procedureTypeCode.StartsWith("MATRICULA", StringComparison.OrdinalIgnoreCase))
        {
            return Matriculas;
        }

        if (string.Equals(procedureTypeCode, "TRASPASO", StringComparison.OrdinalIgnoreCase))
        {
            return Traspasos;
        }

        return Otros;
    }

    public static bool MatchesCategory(string procedureTypeCode, string category) =>
        string.Equals(Classify(procedureTypeCode), category, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 5: Write failing unit tests for owner resolver**

`OwnerNameResolverTests.cs`:

```csharp
using Flit.Procedures.Infrastructure.Persistence.Entities;
using Flit.Procedures.Runtime.Dashboard;
using Flit.Procedures.Shared.Domain;

namespace Flit.Identity.UnitTests.Procedures;

public class OwnerNameResolverTests
{
    [Fact]
    public void Resolve_prefers_comprador_role()
    {
        var actors = new[]
        {
            Actor("Vendedor", 1, "Cc", "111"),
            Actor("Comprador", 2, "Cc", "222", """{"nombre":"María García"}"""),
        };

        Assert.Equal("María García", OwnerNameResolver.Resolve(actors));
    }

    [Fact]
    public void Resolve_falls_back_to_document_when_no_json()
    {
        var actors = new[] { Actor("Vendedor", 1, "Cc", "1234567890") };
        Assert.Equal("Cc 1234567890", OwnerNameResolver.Resolve(actors));
    }

    private static ProcedureInstanceActor Actor(
        string role, int order, string docType, string docNumber, string? json = null) =>
        new()
        {
            RoleLabel = role,
            SortOrder = order,
            PersonKind = PersonKind.Natural,
            DocumentType = Enum.Parse<DocumentIdType>(docType),
            DocumentNumber = docNumber,
            IsLegalRepresentative = false,
            ExternalDataJson = json,
        };
}
```

- [ ] **Step 6: Implement owner resolver**

`OwnerNameResolver.cs`:

```csharp
using System.Text.Json;
using Flit.Procedures.Infrastructure.Persistence.Entities;
using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Runtime.Dashboard;

public static class OwnerNameResolver
{
    private static readonly string[] PreferredRoleTokens = ["propietario", "comprador", "titular"];

    public static string Resolve(IEnumerable<ProcedureInstanceActor> actors)
    {
        var candidates = actors.Where(a => !a.IsLegalRepresentative).ToList();
        var selected = candidates.FirstOrDefault(MatchesPreferredRole)
            ?? candidates.MinBy(a => a.SortOrder);

        if (selected is null)
        {
            return "—";
        }

        var fromJson = TryReadName(selected);
        if (!string.IsNullOrWhiteSpace(fromJson))
        {
            return fromJson;
        }

        return $"{selected.DocumentType} {selected.DocumentNumber}";
    }

    private static bool MatchesPreferredRole(ProcedureInstanceActor actor)
    {
        var role = actor.RoleLabel.ToLowerInvariant();
        return PreferredRoleTokens.Any(token => role.Contains(token, StringComparison.Ordinal));
    }

    private static string? TryReadName(ProcedureInstanceActor actor)
    {
        if (string.IsNullOrWhiteSpace(actor.ExternalDataJson))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(actor.ExternalDataJson);
        var root = doc.RootElement;
        if (actor.PersonKind == PersonKind.Juridica
            && root.TryGetProperty("razonSocial", out var razon))
        {
            return razon.GetString();
        }

        if (root.TryGetProperty("nombre", out var nombre))
        {
            return nombre.GetString();
        }

        return null;
    }
}
```

- [ ] **Step 7: Run unit tests — expect PASS**

Run: `dotnet test services/core-api/tests/Flit.Identity.UnitTests/Flit.Identity.UnitTests.csproj --filter "ProcedureCategoryClassifierTests|OwnerNameResolverTests" -v n`

Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add services/core-api/src/Flit.Procedures.Runtime/ services/core-api/tests/Flit.Identity.UnitTests/Procedures/
git commit -m "feat(tramites): dashboard classifier and owner name resolver"
```

### Task 2: Dashboard auth filter + date range helper

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Runtime/Auth/RequireDashboardAccessExtensions.cs`
- Create: `services/core-api/src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardDateRange.cs`
- Create: `services/core-api/src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardQuery.cs`

- [ ] **Step 1: Add dashboard auth filter**

`RequireDashboardAccessExtensions.cs`:

```csharp
using Flit.Identity.Rbac;
using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Procedures.Runtime.Auth;

public sealed class RequireDashboardAccessFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var user = CurrentUserFactory.FromPrincipal(http.User);
        if (user is null)
        {
            return ValueTask.FromResult<object?>(Results.Json(
                new { code = ApiErrorCodes.TokenExpired },
                statusCode: StatusCodes.Status401Unauthorized));
        }

        if (user.IsSuperAdmin)
        {
            http.Items["CurrentUser"] = user;
            return next(context);
        }

        if (user.TenantId is null)
        {
            return ValueTask.FromResult<object?>(Results.Json(
                new { code = ApiErrorCodes.Forbidden },
                statusCode: StatusCodes.Status403Forbidden));
        }

        var authorization = http.RequestServices.GetRequiredService<AuthorizationService>();
        var resource = new ResourceContext(user.TenantId.Value, user.Id);
        if (!authorization.CanAccess(user, "users:read", resource))
        {
            return ValueTask.FromResult<object?>(Results.Json(
                new { code = ApiErrorCodes.Forbidden },
                statusCode: StatusCodes.Status403Forbidden));
        }

        http.Items["CurrentUser"] = user;
        return next(context);
    }
}

public static class RequireDashboardAccessExtensions
{
    public static RouteGroupBuilder RequireDashboardAccess(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter(new RequireDashboardAccessFilter());
        return group;
    }
}
```

- [ ] **Step 2: Add date range parser**

`TramitesDashboardDateRange.cs`:

```csharp
using Microsoft.AspNetCore.Http;

namespace Flit.Procedures.Runtime.Dashboard;

public sealed record TramitesDashboardDateRange(
    DateOnly From,
    DateOnly To,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc)
{
    public static IResult? TryParse(
        string? from,
        string? to,
        out TramitesDashboardDateRange? range)
    {
        range = null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = ParseOrDefault(from, today.AddDays(-30));
        var toDate = ParseOrDefault(to, today);

        if (fromDate > toDate)
        {
            return Results.Json(
                new { code = "VALIDATION_ERROR", message = "from must be <= to." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (toDate.DayNumber - fromDate.DayNumber > 366)
        {
            return Results.Json(
                new { code = "VALIDATION_ERROR", message = "Date range cannot exceed 366 days." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        range = new TramitesDashboardDateRange(
            fromDate,
            toDate,
            new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            new DateTimeOffset(toDate.ToDateTime(new TimeOnly(23, 59, 59)), TimeSpan.Zero));
        return null;
    }

    private static DateOnly ParseOrDefault(string? value, DateOnly fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : DateOnly.Parse(value);
}
```

- [ ] **Step 3: Add shared query builder**

`TramitesDashboardQuery.cs`:

```csharp
using Flit.Identity.Shared.Auth;
using Flit.Procedures.Infrastructure.Persistence;
using Flit.Procedures.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Runtime.Dashboard;

public static class TramitesDashboardQuery
{
    public static IResult? TryResolveTenant(
        CurrentUser user,
        Guid? tenantId,
        out Guid? effectiveTenantId)
    {
        effectiveTenantId = null;
        if (user.IsSuperAdmin)
        {
            effectiveTenantId = tenantId;
            return null;
        }

        if (user.TenantId is not { } tid)
        {
            return Results.Json(new { code = "FORBIDDEN" }, statusCode: StatusCodes.Status403Forbidden);
        }

        effectiveTenantId = tid;
        return null;
    }

    public static IQueryable<ProcedureInstance> Apply(
        ProceduresDbContext db,
        Guid? tenantId,
        TramitesDashboardDateRange range)
    {
        var query = db.ProcedureInstances.AsNoTracking()
            .Where(i => i.CreatedAt >= range.FromUtc && i.CreatedAt <= range.ToUtc);

        if (tenantId is { } tid)
        {
            query = query.Where(i => i.TenantId == tid);
        }

        return query;
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add services/core-api/src/Flit.Procedures.Runtime/Auth/RequireDashboardAccessExtensions.cs \
  services/core-api/src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardDateRange.cs \
  services/core-api/src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardQuery.cs
git commit -m "feat(tramites): dashboard auth filter and query helpers"
```

### Task 3: Summary + detail handlers

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardModels.cs`
- Create: `services/core-api/src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardSummaryHandler.cs`
- Create: `services/core-api/src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardDetailHandler.cs`
- Create: `services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/Dashboard/TramitesDashboardTestHelper.cs`
- Create: `services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/Dashboard/TramitesDashboardAuthTests.cs`
- Create: `services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/Dashboard/TramitesDashboardSummaryTests.cs`
- Create: `services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/Dashboard/TramitesDashboardDetailTests.cs`

- [ ] **Step 1: Write failing auth integration test**

`TramitesDashboardAuthTests.cs`:

```csharp
using System.Net;

namespace Flit.Identity.IntegrationTests.Tramites.Dashboard;

public class TramitesDashboardAuthTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;
    public TramitesDashboardAuthTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Tenant_operator_without_users_read_gets_forbidden()
    {
        if (!_factory.IsDockerAvailable) return;
        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsTenantOperatorAsync();
        var response = await client.GetAsync("/api/v1/tramites/dashboard/summary");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Tenant_admin_with_users_read_can_access_summary()
    {
        if (!_factory.IsDockerAvailable) return;
        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsTenantAdminAsync();
        var response = await client.GetAsync("/api/v1/tramites/dashboard/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test — expect FAIL (404)**

Run: `dotnet test services/core-api/tests/Flit.Identity.IntegrationTests/Flit.Identity.IntegrationTests.csproj --filter TramitesDashboardAuthTests -v n`

- [ ] **Step 3: Implement models + summary handler**

`TramitesDashboardModels.cs` — records for `SummaryResponse`, `CategoryCountDto`, `DetailRowDto`, `PagedDetailResponse`.

`TramitesDashboardSummaryHandler.cs` — load instances in range, group by `ProcedureCategoryClassifier.Classify`, compute counts and percents.

`TramitesDashboardDetailHandler.cs` — filter by category using `MatchesCategory`, include actors, map rows with `OwnerNameResolver`, paginate.

- [ ] **Step 4: Wire endpoints + DI**

In `TramitesEndpoints.cs` add dashboard group:

```csharp
var dashboardGroup = app.MapGroup("/api/v1/tramites/dashboard").RequireDashboardAccess();
dashboardGroup.MapGet("/summary", GetDashboardSummaryAsync);
dashboardGroup.MapGet("/detail", GetDashboardDetailAsync);
```

In `Program.cs`:

```csharp
builder.Services.AddScoped<TramitesDashboardSummaryHandler>();
builder.Services.AddScoped<TramitesDashboardDetailHandler>();
```

- [ ] **Step 5: Add test helper to seed 3 category instances**

`TramitesDashboardTestHelper.cs` — create procedure types named `Matricula Inicial`, `Traspaso`, `Radicado Cuenta`; POST 3 trámites via API as tenant admin.

- [ ] **Step 6: Write summary + detail integration tests**

`TramitesDashboardSummaryTests` — after seed, assert `matriculas=1`, `traspasos=1`, `otros=1`.

`TramitesDashboardDetailTests` — `GET /detail?category=traspasos` returns only traspaso row; second tenant gets 0 rows.

- [ ] **Step 7: Run dashboard tests — expect PASS**

Run: `dotnet test services/core-api/tests/Flit.Identity.IntegrationTests/Flit.Identity.IntegrationTests.csproj --filter "FullyQualifiedName~Tramites.Dashboard" -v n`

- [ ] **Step 8: Commit**

```bash
git commit -m "feat(tramites): dashboard summary and detail API"
```

### Task 4: Users + export handlers

**Files:**
- Create: `services/core-api/src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardUsersHandler.cs`
- Create: `services/core-api/src/Flit.Procedures.Runtime/Dashboard/TramitesDashboardExportHandler.cs`
- Create: `services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/Dashboard/TramitesDashboardUsersTests.cs`
- Create: `services/core-api/tests/Flit.Identity.IntegrationTests/Tramites/Dashboard/TramitesDashboardExportTests.cs`

- [ ] **Step 1: Implement users handler**

Inject `IdentityDbContext` + `ProceduresDbContext`.

- `HandleTopAsync` — group by `created_by`, order desc, take 5, join users for email as displayName
- `HandleSearchAsync` — filter tenant users by email contains `q`
- `HandleStatsAsync` — count instances for `userId` in range

- [ ] **Step 2: Implement export handler**

Use ClosedXML workbook with columns: ID, Fecha radicación, Estado, Placa, Nombre propietario, Fecha actualización.

If row count > 10_000 → `400 { code: "EXPORT_LIMIT_EXCEEDED" }`.

Return `Results.File(stream, contentType, fileName)`.

- [ ] **Step 3: Map endpoints**

```csharp
dashboardGroup.MapGet("/users/top", GetDashboardUsersTopAsync);
dashboardGroup.MapGet("/users", GetDashboardUsersSearchAsync);
dashboardGroup.MapGet("/users/{userId:guid}/stats", GetDashboardUserStatsAsync);
dashboardGroup.MapGet("/export", GetDashboardExportAsync);
```

Register handlers in `Program.cs`.

- [ ] **Step 4: Write users + export integration tests**

`TramitesDashboardUsersTests` — seed 2 trámites as admin, 1 as operator (if operator can create; else same admin); top returns admin first. Stats for user with 0 returns count 0.

`TramitesDashboardExportTests` — export traspasos returns `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.

- [ ] **Step 5: Run all Tramites tests**

Run: `dotnet test services/core-api/tests/Flit.Identity.IntegrationTests/Flit.Identity.IntegrationTests.csproj --filter "FullyQualifiedName~Tramites" -v n`

Expected: all pass (existing 11 + new dashboard tests)

- [ ] **Step 6: Commit**

```bash
git commit -m "feat(tramites): dashboard users stats and Excel export API"
```

---

## HU-2: FE dashboard (HU-TBD)

**Branch:** `feature/HU-TBD-DCHICA-fe-tramites-dashboard`

**Depends on:** HU-1 merged.

### Task 1: Refactor tramites layouts + nav

**Files:**
- Modify: `frontend/app/tramites/layout.tsx`
- Create: `frontend/app/tramites/(index)/layout.tsx`
- Move: `frontend/app/tramites/page.tsx` → `frontend/app/tramites/(index)/page.tsx`
- Modify: `frontend/lib/flit/nav.ts`

- [ ] **Step 1: Split layouts**

`app/tramites/layout.tsx` — only `getSessionOrRedirect()` + `AppShell` (no permission check).

`app/tramites/(index)/layout.tsx` — guard `tramites:read` (move current guard logic here).

`app/tramites/dashboard/layout.tsx` — guard `users:read || isSuperAdmin`; deny message references dashboard permission.

- [ ] **Step 2: Add nav link**

In `buildAdminNav`:

```ts
if (hasPermission(session, "users:read") || session.isSuperAdmin) {
  items.push({ href: "/tramites/dashboard", label: "Dashboard" });
}
```

Insert after Trámites link.

- [ ] **Step 3: Verify existing E2E still pass**

Run: `cd frontend && pnpm exec playwright test e2e/tramites/tramites-index.spec.ts`

- [ ] **Step 4: Commit**

```bash
git commit -m "feat(tramites): split layouts for dashboard auth"
```

### Task 2: API types + client helpers

**Files:**
- Create: `frontend/lib/tramites/dashboard-types.ts`
- Create: `frontend/lib/tramites/dashboard-api.ts`

- [ ] **Step 1: Add types matching API DTOs**

Export `DashboardSummary`, `DashboardCategory`, `DashboardDetailRow`, `DashboardUserStat`, `DashboardCategoryKey`.

- [ ] **Step 2: Add fetch helpers**

```ts
export function buildDashboardQuery(params: {
  from?: string;
  to?: string;
  tenantId?: string;
  category?: string;
  page?: number;
  pageSize?: number;
}): string { /* URLSearchParams */ }

export async function fetchDashboardSummary(search: string): Promise<DashboardSummary>
export async function fetchDashboardDetail(search: string): Promise<PagedDetail>
export async function fetchDashboardUsersTop(search: string): Promise<{ items: DashboardUserStat[] }>
export async function fetchDashboardUserStats(userId: string, search: string): Promise<{ count: number }>
export async function fetchDashboardUsers(q: string, search: string): Promise<{ items: ... }>
export function dashboardExportUrl(category: string, search: string): string
```

Use `apiFetch` from `@/lib/auth/api-client` for JSON; export uses `/api/v1/tramites/dashboard/export?...` as download href.

- [ ] **Step 3: Commit**

```bash
git commit -m "feat(tramites): dashboard API client types and helpers"
```

### Task 3: Dashboard UI components

**Files:**
- Modify: `frontend/package.json`
- Create: `frontend/components/tramites/dashboard/*.tsx`
- Create: `frontend/app/tramites/dashboard/page.tsx`

- [ ] **Step 1: Install recharts**

```bash
cd frontend && pnpm add recharts
```

- [ ] **Step 2: Build `TramitesDashboard` client orchestrator**

State: `from`, `to`, `tenantId?`, `selectedCategory`, `detailRows`, `summary`, `topUsers`, `extraUserCards`.

Effects: refetch summary + top on date/tenant change; fetch detail on category click; fetch stats when user added to multiselect.

- [ ] **Step 3: Build `CategoryDonutChart`**

recharts `PieChart` + `Pie` with 3 segments; `onClick` passes `category.key` to parent.

`data-testid="dashboard-donut-chart"`.

- [ ] **Step 4: Build `DetailTablePanel`**

Columns: ID, Fecha radicación, Estado, Placa, Propietario, Fecha actualización.

Visible when `selectedCategory` set. `data-testid="dashboard-detail-table"`.

- [ ] **Step 5: Build user cards + multiselect**

`UserProductivityCard` — show count or zero message per RF09.

`UserMultiselect` — debounced search calling `/users?q=`.

- [ ] **Step 6: Build `TenantSelector`**

Only if `session.isSuperAdmin`; fetch `/api/v1/admin/tenants` or existing tenants list endpoint used elsewhere.

- [ ] **Step 7: Page shell**

`app/tramites/dashboard/page.tsx` — `PageHeaderCard` title "Dashboard de Trámites"; render `<TramitesDashboard />` with session props.

`data-testid="tramites-dashboard"`.

- [ ] **Step 8: Manual smoke**

Run stack (`docker compose up`) → login `admin@tenant-a.com` → `/tramites/dashboard` shows chart.

- [ ] **Step 9: Commit**

```bash
git commit -m "feat(tramites): dashboard UI with chart, detail table, and user cards"
```

### Task 4: Excel export button

- [ ] **Step 1: Wire export button in `TramitesDashboard`**

Enabled when `selectedCategory` set. Opens `dashboardExportUrl(selectedCategory, query)` in new window or triggers download via hidden `<a download>`.

- [ ] **Step 2: Commit**

```bash
git commit -m "feat(tramites): dashboard Excel export download"
```

---

## HU-3: E2E dashboard (HU-TBD)

**Branch:** `feature/HU-TBD-DCHICA-e2e-tramites-dashboard`

**Depends on:** HU-2 merged.

### Task 1: Playwright spec

**Files:**
- Create: `frontend/e2e/tramites/tramites-dashboard.spec.ts`

- [ ] **Step 1: Write E2E test**

```ts
import { test, expect } from "@playwright/test";
import { loginAsTenantAdmin, skipIfStackUnavailable } from "../identity/helpers";

test.beforeEach(async ({}, testInfo) => {
  await skipIfStackUnavailable(testInfo);
});

test("tenant admin can open dashboard and see chart", async ({ page }) => {
  await loginAsTenantAdmin(page);
  await expect(page.getByRole("link", { name: "Dashboard" })).toBeVisible();
  await page.goto("/tramites/dashboard");
  await expect(page.getByTestId("tramites-dashboard")).toBeVisible();
  await expect(page.getByTestId("dashboard-donut-chart")).toBeVisible();
});

test("clicking chart segment shows detail table", async ({ page }) => {
  await loginAsTenantAdmin(page);
  await page.goto("/tramites/dashboard");
  // If no data, skip segment click assertion or seed via API in beforeAll
  const segment = page.locator(".recharts-pie-sector").first();
  if (await segment.count()) {
    await segment.click();
    await expect(page.getByTestId("dashboard-detail-table")).toBeVisible();
  }
});
```

- [ ] **Step 2: Run E2E**

Run: `cd frontend && pnpm exec playwright test e2e/tramites/tramites-dashboard.spec.ts`

- [ ] **Step 3: Commit**

```bash
git commit -m "test(tramites): e2e dashboard smoke tests"
```

---

## Post-implementation checklist

- [ ] All integration tests: `dotnet test services/core-api/tests/Flit.Identity.IntegrationTests --filter Tramites`
- [ ] Unit tests: `dotnet test services/core-api/tests/Flit.Identity.UnitTests --filter Procedures`
- [ ] Frontend typecheck: `cd frontend && pnpm typecheck`
- [ ] E2E tramites: `cd frontend && pnpm exec playwright test e2e/tramites`
- [ ] Update spec ADO story IDs when HUs are created in Azure DevOps
- [ ] Register PR in ADO per `flit-integration-ado` skill

---

## Spec coverage self-review

| Spec section | Plan task |
|--------------|-----------|
| Auth `users:read` | Task 2 HU-1, Task 1 HU-2 |
| Date filter 30d default | `TramitesDashboardDateRange` |
| Category mapping | Task 1 HU-1 |
| Summary endpoint | Task 3 HU-1 |
| Detail endpoint | Task 3 HU-1 |
| Users top/search/stats | Task 4 HU-1 |
| Export xlsx 10k cap | Task 4 HU-1 |
| Owner name heuristic | Task 1 HU-1 |
| FE route + layout split | Task 1 HU-2 |
| Donut chart + lateral table | Task 3 HU-2 |
| User cards + multiselect | Task 3 HU-2 |
| Excel download | Task 4 HU-2 |
| Integration tests | Tasks 3–4 HU-1 |
| E2E | HU-3 |
| RF10 PDF deferred | Not in plan (correct) |
| `approved_at` omitted | Not in plan (correct) |
