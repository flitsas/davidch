# Identity MVP Hardening — Feature #9548 Closure

**Date:** 2026-06-10  
**Status:** Approved  
**Parent spec:** `docs/superpowers/specs/2026-06-09-identity-jwt-rbac-design.md`  
**ADO Feature:** [#9548](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9548)  
**Approach:** Balanced MVP closure (Option A)

## Summary

Close Feature #9548 by fixing confirmed backend bugs (4 failing integration tests), extending the users list API with `role_ids`, completing frontend parity for all **existing** backend endpoints, and adding a minimal E2E suite (CF-I3–I5). Production hardening (Redis rate limits, refresh reuse detection, YARP in Docker, KMS keys) is explicitly deferred.

## Locked decisions

| Area | Decision |
|------|----------|
| Priority | MVP closure — unblock stories 9712–9720 |
| API surface | No new `GET/PUT /api/users/{id}`; extend `GET /api/users` with `role_ids` |
| Routing (dev) | Next.js rewrites to API (`:3000` → `:5080`); no gateway in Docker for MVP |
| UI scope | Functional admin UI with existing Tailwind patterns; no design-system pass |
| Tests gate | 16/16 integration tests + 4 Playwright E2E tests |

## Non-goals (deferred)

- Refresh token reuse detection (token family revocation)
- Distributed rate limiting / `token_version` cache (Redis)
- YARP gateway in `docker-compose.yml`
- `tenants.is_active` login guard
- EF global query filter on `users`
- KMS/HSM for JWT signing keys
- Full Web Interface Guidelines / FLIT design tokens audit
- ADR documents (write after MVP merge)

---

## 1. Backend bug fixes

### Root causes (confirmed by test run)

| Failure | Root cause |
|---------|------------|
| `GET /api/auth/me` → 500 | `MeHandler` reads `sub` only via `JwtRegisteredClaimNames.Sub`; JWT middleware maps it to `ClaimTypes.NameIdentifier`, so `Guid.Parse(null)` throws |
| Session eviction → 500 instead of 403 | `TokenVersionValidationMiddleware` skips version check when `sub` is null; request reaches `MeHandler` and throws |
| `POST /api/auth/reset-password` → 400 | Client sends `new_password` (snake_case); `ResetPasswordRequest` has no `[JsonPropertyName]` binding |

### Fix: shared claim helper

Add `ClaimPrincipalExtensions.GetUserId()` in `Flit.Identity.Shared/Auth/`:

```csharp
public static Guid? GetUserId(this ClaimsPrincipal principal)
{
    var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
    return Guid.TryParse(sub, out var id) ? id : null;
}
```

Apply in:
- `MeHandler.cs`
- `TokenVersionValidationMiddleware.cs`
- `TenantResolutionMiddleware.cs` (replace inline parsing)

### Fix: auth DTO JSON binding

Add `[JsonPropertyName("snake_case")]` on public auth request records used by the API and frontend:

| Record | Properties |
|--------|------------|
| `ResetPasswordRequest` | `token`, `new_password` |
| `ActivateRequest` | `token`, `password` |
| `LoginRequest` | `email`, `password` (already lowercase — verify) |
| `ForgotPasswordRequest` | `email` |

### Acceptance

```bash
cd backend && dotnet test Flit.Identity.sln
# Expected: 6 unit + 16 integration = 22/22 pass
```

---

## 2. API extension — users list with roles

### Change

Extend `GET /api/users` response item:

```json
{
  "id": "uuid",
  "email": "user@tenant.com",
  "status": "Active",
  "role_ids": ["uuid-1", "uuid-2"],
  "tenant_id": "uuid",
  "created_at": "2026-06-10T00:00:00Z",
  "activated_at": "2026-06-10T00:00:00Z"
}
```

### Implementation

In `UsersEndpoints.ListUsersAsync`:
- Include `UserRoles` in query (or subquery)
- Project `role_ids` as `Guid[]` per user
- No new endpoint, no breaking change to existing fields

### Acceptance

- Postman / integration test: list returns `role_ids` for users with assigned roles
- Empty array for users with no roles

---

## 3. Frontend parity

All mutations use existing `fetch("/api/...")` with `credentials: "include"` (Next.js rewrites proxy to backend).

### 3.1 Logout

- Add logout button in `admin/layout.tsx` header and home page header
- `POST /api/auth/logout` → `router.push("/login")` + `router.refresh()`

### 3.2 Admin users page

| Action | Endpoint | UI |
|--------|----------|-----|
| List with roles | `GET /api/users` | Pass `role_ids` to `UserRolesEditor` |
| Invite | `POST /api/users/invite` | Existing form; add tenant `<select>` when `session.isSuperAdmin` |
| Assign roles | `PUT /api/users/{id}/roles` | Fix `currentRoleIds` from list data |
| Force reset | `POST /api/users/{id}/force-reset` | Per-user button + confirm |
| Block user | `POST /api/users/{id}/block` | Per-user button + confirm |

### 3.3 SuperAdmin tenant picker (CF-C4)

When `session.isSuperAdmin`:
- Fetch tenants list — **requires** `GET /api/tenants` **OR** pass tenant from session context only

**Decision:** For MVP, add minimal `GET /api/tenants` (SuperAdmin-only, returns `{ id, name, slug }[]`) since SuperAdmin has `tenant_id = null` and cannot infer tenant from session. This is a small additive endpoint justified by CF-C4.

> **Scope note:** This is the one new endpoint in MVP hardening — read-only, SuperAdmin-gated, no CRUD.

### 3.4 Role migration UX (CF-G3)

Replace `prompt("ID del rol de reemplazo")` in `RolePermissionsEditor`:
- Dropdown populated from `GET /api/roles` (exclude current role)
- Confirm dialog showing affected user count (from 409 response `affected_users`)

### 3.5 Session revoked flow (CF-H2)

| Layer | Behavior |
|-------|----------|
| `apiFetch` (server) | On 403 + `SESSION_REVOKED` → throw `SessionRevokedError` |
| Server pages | Catch error → `redirect("/login?reason=session_revoked")` |
| `middleware.ts` | On refresh failure with `X-Session-Revoked` header → redirect with reason param |
| Login page | Already shows `SessionRevokedBanner` when `reason=session_revoked` |

### 3.6 Type updates

```typescript
// lib/admin/types.ts
export type UserSummary = {
  // ...existing fields
  roleIds: string[];  // maps from role_ids
};
```

### Acceptance

Manual walkthrough against `docker compose up`:
1. SuperAdmin login → invite user (with tenant pick) → Mailhog link → activate → login
2. Admin assigns roles → user re-login sees updated permissions
3. Admin force-resets user → old session invalidated
4. Admin blocks user → user cannot login
5. Logout clears session

---

## 4. Minimal E2E suite (story #9720)

**Target:** `http://localhost:3000` (compose stack, no gateway).

| Spec file | Scenario | Criteria |
|-----------|----------|----------|
| `auth-lifecycle.spec.ts` | Super admin login → home | CF-A3 (existing, keep) |
| `invite-activate-login.spec.ts` | Invite → Mailhog token → activate → login | CF-I3 |
| `session-eviction.spec.ts` | Role change → stale session → revoked banner | CF-I4 |
| `role-migration.spec.ts` | Delete role with users → 409 → migrate → deleted | CF-I5 |

### Test infrastructure

- `helpers.ts`: health check against `localhost:3000` and `localhost:5080`
- Env vars: `E2E_ADMIN_EMAIL`, `E2E_ADMIN_PASSWORD` (from `docker/.env`)
- Skip gracefully when stack not running
- Document in `frontend/README.md`

### Acceptance

```bash
cd frontend && npx playwright test e2e/identity
# Expected: 4/4 pass with docker compose running
```

---

## 5. Docker / dev topology (unchanged)

| Service | Port | Role |
|---------|------|------|
| `frontend` | 3000 | UI + `/api/*` rewrite proxy |
| `identity-api` | 5080 | Direct API access (tests, Postman) |
| `postgres` | 5432 | Database |
| `mailhog` | 8025 | Email UI |

Gateway (`:5023`) documented as production concern, not MVP.

---

## 6. Story traceability

| Story | MVP deliverable | Status after hardening |
|-------|-----------------|------------------------|
| [#9711](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9711) | Schema + seed | Done (no change) |
| [#9712](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9712) | Login/refresh/logout/me bugs fixed | **Close** |
| [#9713](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9713) | RBAC + tenant filter | Done (no change) |
| [#9714](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9714) | SuperAdmin tenant picker + `GET /api/tenants` | **Close** |
| [#9715](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9715) | Force-reset UI + JSON binding | **Close** |
| [#9716](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9716) | Middleware + MeHandler fix | **Close** |
| [#9717](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9717) | Role migration dropdown | **Close** |
| [#9718](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9718) | Logout + session revoked flow | **Close** |
| [#9719](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9719) | Full admin parity | **Close** |
| [#9720](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9720) | 4 E2E tests | **Close** |

---

## 7. Implementation order

```
Phase 1 — Backend fixes (blocks everything)
  ├── ClaimPrincipalExtensions + middleware/MeHandler fixes
  ├── Auth DTO JsonPropertyName attributes
  └── Verify 22/22 tests pass

Phase 2 — API extensions
  ├── GET /api/users with role_ids
  └── GET /api/tenants (SuperAdmin read-only)

Phase 3 — Frontend parity
  ├── Types + UserRolesEditor fix
  ├── Admin users actions (force-reset, block, tenant picker)
  ├── Role migration dropdown
  ├── Logout + session revoked handling
  └── Manual walkthrough

Phase 4 — E2E
  ├── invite-activate-login.spec.ts
  ├── session-eviction.spec.ts
  ├── role-migration.spec.ts
  └── README documentation
```

---

## 8. Risk register

| Risk | Mitigation |
|------|------------|
| `GET /api/tenants` is new scope | Minimal read-only, SuperAdmin-gated; justified by CF-C4 |
| E2E flakiness (Mailhog timing) | Poll Mailhog API with timeout; retry token extraction |
| Cookie forwarding in Playwright | Use `credentials: "include"`; test against `:3000` unified origin |
| JSON casing frontend ↔ backend | ASP.NET camelCase default; verify with integration test for `role_ids` |
