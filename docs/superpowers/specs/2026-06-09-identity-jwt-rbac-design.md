# Identity Layer — JWT, RBAC/ABAC & Multi-Tenant Credentials

**Date:** 2026-06-09  
**Status:** Approved  
**ADO Feature:** [#9548](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9548) — [IDENTIDAD] Autenticación JWT, RBAC/ABAC e ciclo de credenciales multi-tenant  
**Project:** FLIT 2.0 — FLIT EVOLUTION

## Summary

Design a homogeneous identity and security layer for FLIT 2.0: native JWT authentication, RBAC with lightweight ABAC scopes, strict multi-tenant isolation, email-based invitation onboarding, credential lifecycle management, and immediate session eviction on privilege changes.

This spec covers all 14 functional requirements (RF01–RF14) and 35 functional criteria (CF-A through CF-I) from Feature #9548. Implementation will be decomposed into user stories after this design is approved.

## Locked decisions

| Area | Decision |
|------|----------|
| Scope | Full feature — all 14 RFs; decompose into stories later |
| Frontend | Next.js 16 App Router (`flit2`) |
| Backend | .NET modular monolith behind YARP gateway |
| Backend modules | Auth, Users, RBAC, Notifications (single deployable) |
| Routing | Unified domain (Approach 1): YARP routes `/api/*` → .NET, `/*` → Next.js |
| Tokens | Access + refresh, **both httpOnly cookies** (`flit_access`, `flit_refresh`) |
| Access token TTL | 15 minutes |
| Refresh token TTL | 7 days (rotated on each refresh) |
| Revocation | Invalidate refresh tokens + bump `users.token_version` |
| Database | PostgreSQL, shared schema, `tenant_id` on tenant-scoped tables |
| ORM | EF Core with global tenant query filters |
| Email | SMTP configurable per environment; Mailhog in dev |
| Authorization | RBAC + lightweight ABAC via `PermissionScope` enum: `Global`, `Tenant`, `Own` |
| Password hashing | Argon2id |
| JWT signing | RS256 (asymmetric) |

## Non-goals (MVP — per Feature #9548)

- SSO / external identity providers (Google, Microsoft, AD, SAML, Auth0)
- MFA / two-factor authentication
- Dynamic hot-code injection for permission logic
- Full ABAC policy engine / DSL

---

## 1. Architecture overview

### System diagram

```
Browser
   │
   ▼
YARP Gateway (app.flit.com)
   ├── /api/*  ──► .NET Modular Monolith
   │                 ├── Auth Module
   │                 ├── Users Module
   │                 ├── RBAC Module
   │                 └── Notifications Module
   └── /*      ──► Next.js 16 (middleware auth guard, server-side API calls)
                       │
                       ▼
                  PostgreSQL          SMTP / Mailhog
```

### Module responsibilities

| Module | Owns |
|--------|------|
| **Auth** | Login, logout, refresh, token issuance, `token_version`, password hash/verify, forgot/reset flows, account activation |
| **Users** | User CRUD, invitation lifecycle (Pending → Active), tenant assignment, multi-role assignment |
| **RBAC** | Roles, permissions catalog (CRUD + UI strings), scope assignment, conflict warnings, role deletion + batch migration |
| **Notifications** | Email templates + SMTP dispatch (invitation, self-service reset, forced reset) |

### Cross-cutting API middleware

1. **Authentication** — read `flit_access` cookie → validate JWT signature, expiry, and `token_version`
2. **Authorization** — check permission claim + `PermissionScope` against resource context
3. **Tenant isolation** — inject `tenant_id` filter on all queries unless SuperAdmin
4. **Session eviction** — stale `token_version` → `403` + `X-Session-Revoked: true` header

### JWT claims (access token)

```json
{
  "sub": "user-uuid",
  "email": "user@tenant.com",
  "tenant_id": "tenant-uuid",
  "roles": ["Operador", "Admin"],
  "permissions": [
    { "key": "tramites:read", "scope": "Tenant" },
    { "key": "tramites:read", "scope": "Own" },
    { "key": "generar_consolidado", "scope": "Tenant" }
  ],
  "is_super_admin": false,
  "token_version": 3,
  "iat": 1710000000,
  "exp": 1710000900
}
```

Permissions are recomputed on login and refresh (multi-role additive union). The browser never reads tokens via JavaScript. Next.js server components and route handlers call `/api/auth/me` for UI permission data.

### Cookie policy

| Cookie | TTL | Flags |
|--------|-----|-------|
| `flit_access` | 15 min | `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/` |
| `flit_refresh` | 7 days | `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/` |

---

## 2. Data model

### Core entities

#### `tenants`

| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID PK | |
| `name` | VARCHAR | Display name |
| `slug` | VARCHAR UNIQUE | URL-safe identifier |
| `is_active` | BOOLEAN | Inactive tenants block login |
| `created_at` | TIMESTAMPTZ | |

#### `users`

| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID PK | |
| `tenant_id` | UUID FK NULL | NULL for SuperAdmin users |
| `email` | VARCHAR UNIQUE | Platform-wide unique |
| `password_hash` | VARCHAR NULL | NULL until account activated |
| `status` | ENUM | `Pending`, `Active`, `Blocked` |
| `token_version` | INT | Default 1; incremented on privilege change |
| `created_at` | TIMESTAMPTZ | |
| `activated_at` | TIMESTAMPTZ NULL | Set on activation |

#### `roles`

| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID PK | |
| `tenant_id` | UUID FK NULL | NULL = system role (SuperAdmin) |
| `name` | VARCHAR | Unique per tenant |
| `is_system` | BOOLEAN | SuperAdmin role: immutable, non-deletable |
| `created_at` | TIMESTAMPTZ | |

#### `permissions`

| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID PK | |
| `key` | VARCHAR UNIQUE | e.g. `tramites:read`, `generar_consolidado` |
| `type` | ENUM | `Crud`, `Ui` |
| `module` | VARCHAR NULL | Required for CRUD; NULL for UI strings |
| `description` | VARCHAR | Admin-facing label |

#### `role_permissions`

| Column | Type | Notes |
|--------|------|-------|
| `role_id` | UUID FK | |
| `permission_id` | UUID FK | |
| `scope` | ENUM | `Global`, `Tenant`, `Own` — only meaningful for CRUD |

Composite PK: `(role_id, permission_id, scope)`

#### `user_roles`

| Column | Type | Notes |
|--------|------|-------|
| `user_id` | UUID FK | |
| `role_id` | UUID FK | |

Composite PK: `(user_id, role_id)`

### Token tables

#### `refresh_tokens`

| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID PK | |
| `user_id` | UUID FK | |
| `token_hash` | VARCHAR | SHA-256 of opaque token |
| `expires_at` | TIMESTAMPTZ | |
| `revoked_at` | TIMESTAMPTZ NULL | |
| `created_at` | TIMESTAMPTZ | |

#### `invitation_tokens`

| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID PK | |
| `user_id` | UUID FK | |
| `token_hash` | VARCHAR | SHA-256 of opaque token |
| `invited_by` | UUID FK | Admin who sent invitation |
| `expires_at` | TIMESTAMPTZ | Default 72 hours |
| `used_at` | TIMESTAMPTZ NULL | Single-use |

#### `password_reset_tokens`

| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID PK | |
| `user_id` | UUID FK | |
| `token_hash` | VARCHAR | SHA-256 of opaque token |
| `type` | ENUM | `SelfService`, `Forced` |
| `expires_at` | TIMESTAMPTZ | SelfService: 1h; Forced: 24h |
| `used_at` | TIMESTAMPTZ NULL | Single-use |

### Invariants

| Rule | Enforcement |
|------|-------------|
| SuperAdmin role is system-level, immutable | `roles.is_system = true`; no UPDATE/DELETE via API |
| Tenant roles never visible cross-tenant | All role queries filter by `tenant_id` |
| Email unique platform-wide | Unique index on `users.email` |
| Pending users cannot login | Auth rejects `status != Active` |
| Multi-role union | Computed at token issuance, not stored |
| Role delete blocked if users assigned | Count guard returns 409 before delete |
| Tenant isolation on business data | EF Core global query filter on `tenant_id` |
| Opaque tokens stored hashed only | Never persist raw invitation/reset/refresh tokens |

### Seed data (initial migration)

- **SuperAdmin** system role with all permissions at `Global` scope
- Bootstrap SuperAdmin user (email/password from environment config)
- Permission catalog seeded per module: CRUD keys (`{module}:create|read|update|delete`) and known UI string permissions

---

## 3. Core flows

### 3.1 Login (RF01, CF-A1–A4)

```
POST /api/auth/login  { email, password }
  1. Look up user by email
  2. Reject if status != Active or password invalid
  3. Verify password against Argon2id hash
  4. Compute effective permissions (multi-role additive union)
  5. Issue access JWT (15 min) + refresh token (7 days)
  6. Set flit_access and flit_refresh httpOnly cookies
  7. Return 200 { id, email, tenant_id, roles[] } — no tokens in body
```

SuperAdmin: `is_super_admin = true`, `tenant_id = null`, all permissions at `Global` scope.

### 3.2 Authenticated session (CF-A3)

```
GET /api/auth/me
  → Requires valid flit_access cookie
  → Returns { user, roles, permissions[] } for UI rendering

POST /api/auth/logout
  → Revoke current refresh token
  → Clear both cookies
  → Return 204
```

Next.js middleware on protected routes: if access cookie missing/expired, attempt silent refresh via `POST /api/auth/refresh`; if that fails, redirect to `/login`.

### 3.3 Token refresh

```
POST /api/auth/refresh  (flit_refresh cookie)
  1. Validate refresh token (not revoked, not expired)
  2. Verify user.token_version matches embedded claim
  3. Recompute permissions (picks up role changes since last refresh)
  4. Rotate refresh token (revoke old, issue new)
  5. Issue new access JWT
  6. Update both cookies
```

### 3.4 Invitation & onboarding (RF03–RF06, CF-C1–C7)

**Admin invites user:**

```
POST /api/users/invite  { email, role_ids[], tenant_id? }
  Tenant Admin: tenant_id inherited from JWT (RF04)
  SuperAdmin:   tenant_id required in body (RF05)

  1. Create user (status=Pending, password_hash=NULL)
  2. Assign roles via user_roles
  3. Generate invitation token (72h expiry)
  4. Send email with activation link via Notifications module
  5. Return 201 { user_id, email, status: "Pending" }
```

**User activates account:**

```
GET  /activate?token=xxx          (Next.js public page)
POST /api/auth/activate           { token, password }
  1. Hash token, look up invitation_tokens
  2. Reject if expired or already used (CF-C6)
  3. Set password_hash, status=Active, activated_at=now()
  4. Mark invitation token used_at
  5. Return 200 → redirect to /login
```

### 3.5 Password management (RF07–RF08, CF-D1–D4)

**Self-service forgot password:**

```
POST /api/auth/forgot-password  { email }
  → Always return 200 regardless of email existence (no enumeration)
  → If user Active: create SelfService reset token (1h), send email

POST /api/auth/reset-password  { token, new_password }
  → Validate token (not expired/used)
  → Update password_hash
  → Increment token_version, revoke all refresh_tokens
  → Mark token used
```

**Admin forced reset:**

```
POST /api/users/{id}/force-reset
  → Requires admin permission
  → Increment token_version
  → Revoke all refresh_tokens for user
  → Create Forced reset token (24h), send email (CF-D4)
  → User must reset via link before next login (CF-D3)
```

### 3.6 Session eviction (RF14, CF-H1–H3)

**Triggers:** role assignment change, role permission change, role deletion/migration, forced reset, admin blocks user.

```
On any privilege change for user U:
  1. U.token_version += 1
  2. Revoke all refresh_tokens for U
  3. Write audit log entry

On next API request with stale JWT:
  → 403 { "code": "SESSION_REVOKED", "message": "..." }
  → Header: X-Session-Revoked: true

Next.js middleware / fetch wrapper:
  → Clear cookies
  → Redirect to /login?reason=session_revoked with alert banner
```

User must re-authenticate to receive updated permissions (CF-H3).

### 3.7 Role management (RF12–RF13, CF-G1–G3)

**Assign roles with conflict detection:**

```
PUT /api/users/{id}/roles  { role_ids[] }
  1. Compute permission union before and after
  2. Detect redundancies (role A permissions ⊂ role B)
  3. Detect conflicts (same permission key with incompatible scopes)
  4. If warnings exist and confirm != true → 200 { warnings[], pending: true }
  5. Apply assignment, bump target user token_version
```

**Delete role with batch migration:**

```
DELETE /api/roles/{id}
  → If user_roles count > 0 → 409 { affected_users: N, code: "ROLE_HAS_USERS" }

POST /api/roles/{id}/migrate  { replacement_role_id }
  1. Reassign all users from source role to replacement role
  2. Delete source role
  3. Bump token_version for all affected users
```

---

## 4. Authorization model

### Permission types

| Type | Key pattern | Scope applies? | Example |
|------|-------------|----------------|---------|
| **CRUD** | `{module}:{action}` | Yes | `tramites:read` with scope `Own` |
| **UI** | `{action_key}` | Always `Tenant` | `generar_consolidado` |

CRUD actions: `create`, `read`, `update`, `delete`.

| Business term | Permission mapping |
|---------------|-------------------|
| Crear | `{module}:create`, scope `Tenant` |
| Editar | `{module}:update`, scope `Tenant` |
| Eliminar | `{module}:delete`, scope `Tenant` |
| Consultar propio | `{module}:read`, scope `Own` |
| Consultar todos | `{module}:read`, scope `Tenant` |

SuperAdmin bypass: `is_super_admin` claim skips all permission and tenant checks (CF-B1, CF-B2, CF-I2). Bypass events are audit-logged.

### Evaluation algorithm

```
CanAccess(user, permissionKey, resource?):
  if user.is_super_admin → return true

  grants = user.permissions where key == permissionKey
  if grants is empty → return false

  for each grant in grants:
    switch grant.scope:
      Global → return true        // SuperAdmin path only
      Tenant → return resource.tenant_id == user.tenant_id
      Own    → return resource.tenant_id == user.tenant_id
                 AND resource.owner_id == user.id

  return false
```

Multi-role additive (RF11, CF-F3, CF-F4): permissions unioned at JWT build; action allowed if **any** role grants it.

### Tenant query filter (EF Core)

Applied to all `ITenantScoped` entities. SuperAdmin context sets `CurrentTenantId = null`, disabling the filter.

```csharp
modelBuilder.Entity<T>()
  .HasQueryFilter(e =>
    _tenantContext.CurrentTenantId == null
    || e.TenantId == _tenantContext.CurrentTenantId);
```

Roles are tenant-scoped (`roles.tenant_id`); roles created in one tenant are never visible in another (CF-E3).

---

## 5. API surface

All endpoints served under `/api/*` via YARP.

### Auth (public unless noted)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/login` | Public | Login, set cookies |
| POST | `/api/auth/logout` | Required | Revoke refresh, clear cookies |
| POST | `/api/auth/refresh` | Refresh cookie | Rotate tokens |
| GET | `/api/auth/me` | Required | Current user + permissions |
| POST | `/api/auth/forgot-password` | Public | Send reset email |
| POST | `/api/auth/reset-password` | Public | Complete password reset |
| POST | `/api/auth/activate` | Public | Complete invitation onboarding |

### Users

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/api/users` | `users:read` Tenant | List users in tenant |
| POST | `/api/users/invite` | `users:create` Tenant | Invite user by email |
| GET | `/api/users/{id}` | `users:read` Tenant/Own | Get user detail |
| PUT | `/api/users/{id}` | `users:update` Tenant | Update user |
| PUT | `/api/users/{id}/roles` | `users:update` Tenant | Assign roles |
| POST | `/api/users/{id}/force-reset` | `users:update` Tenant | Force password reset |

### Roles & permissions

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/api/roles` | `roles:read` Tenant | List tenant roles |
| POST | `/api/roles` | `roles:create` Tenant | Create role |
| GET | `/api/roles/{id}` | `roles:read` Tenant | Get role + permissions |
| PUT | `/api/roles/{id}` | `roles:update` Tenant | Update role name |
| PUT | `/api/roles/{id}/permissions` | `roles:update` Tenant | Set role permissions |
| DELETE | `/api/roles/{id}` | `roles:delete` Tenant | Delete (blocked if users assigned) |
| POST | `/api/roles/{id}/migrate` | `roles:delete` Tenant | Batch reassign + delete |
| GET | `/api/permissions` | `roles:read` Tenant | Permission catalog (read-only) |

---

## 6. Frontend integration (Next.js)

| Concern | Approach |
|---------|----------|
| Auth guard | Middleware checks `flit_access` cookie; silent refresh if expired |
| No JS token access | All API calls from server components / route handlers forward cookies |
| Client components | Receive permissions as props from server parent |
| UI permission gating | `<Can permission="generar_consolidado">` wrapper fed by server context |
| 403 handling | Detect `SESSION_REVOKED` code → clear session, redirect with alert |
| Public routes | `/login`, `/activate`, `/forgot-password`, `/reset-password` |
| Admin routes | `/admin/users`, `/admin/roles` — middleware + server-side permission check |

### UI permission rendering (CF-F1, CF-F2)

Server component fetches `/api/auth/me`, passes `permissions[]` to client tree. Button/action components check:

```tsx
// Server wrapper passes permissions down
<Can permission="generar_consolidado" permissions={permissions}>
  <GenerateButton />
</Can>
```

---

## 7. Error handling & security

| Concern | Decision |
|---------|----------|
| Password hashing | Argon2id |
| JWT signing | RS256 asymmetric; private key in .NET Auth module only |
| CSRF | SameSite=Strict cookies; CSRF token on mutating public forms if needed |
| Rate limiting | Login + forgot-password: 5 attempts / 15 min per IP + email |
| Email enumeration | Forgot-password always returns 200 |
| Audit logging | Privilege changes, login failures, SuperAdmin bypass (CF-I2) |
| Horizontal tenant leak prevention | Integration tests per module (CF-I1) |
| Token storage | Never in localStorage/sessionStorage; httpOnly cookies only |

### Standard error codes

| HTTP | Code | When |
|------|------|------|
| 401 | `INVALID_CREDENTIALS` | Wrong email/password |
| 401 | `TOKEN_EXPIRED` | Access JWT expired, refresh failed |
| 403 | `SESSION_REVOKED` | Stale token_version |
| 403 | `FORBIDDEN` | Missing permission |
| 409 | `ROLE_HAS_USERS` | Delete role with assigned users |
| 422 | `VALIDATION_ERROR` | Invalid input |
| 429 | `RATE_LIMITED` | Too many login/reset attempts |

---

## 8. Testing strategy

| Test | Criteria covered |
|------|-----------------|
| E2E: invite → activate → login → permitted action | CF-I3 |
| E2E: change permissions → next request 403 → re-login | CF-I4 |
| E2E: delete role with users → 409 → migrate → success | CF-I5 |
| Unit: multi-role permission union | CF-F3, CF-F4 |
| Unit: scope evaluation (Own / Tenant / Global) | CF-E1, CF-E2 |
| Unit: SuperAdmin bypass + audit | CF-B1, CF-B2, CF-I2 |
| Integration: tenant filter prevents cross-tenant reads | CF-I1, CF-E3 |
| Integration: token_version bump revokes session | CF-H1, CF-H2, CF-H3 |
| Integration: forced reset invalidates active session | CF-D3 |
| Integration: invitation token expiry rejection | CF-C6 |

---

## 9. Story decomposition (post-design)

Recommended implementation order for user story breakdown:

| Phase | Stories | RFs |
|-------|---------|-----|
| **1 — Foundation** | DB schema + seed, Auth login/logout/refresh, middleware | RF01, RF02 |
| **2 — RBAC core** | Roles, permissions catalog, scope evaluation, tenant filter | RF09, RF10, RF11 |
| **3 — User lifecycle** | Invitation, activation, user admin UI | RF03–RF06 |
| **4 — Credentials** | Forgot/reset password, forced reset | RF07, RF08 |
| **5 — Session control** | token_version eviction, Next.js 403 handling | RF14 |
| **6 — Role admin** | Conflict warnings, role delete + migration UI | RF12, RF13 |
| **7 — QA** | E2E suite for CF-I3–I5 | CF-I1–I5 |

---

## 10. Local development setup

| Component | Dev config |
|-----------|------------|
| YARP | Route `localhost:5000/api/*` → .NET, `localhost:5000/*` → Next.js `:3000` |
| PostgreSQL | Docker container, connection string in `appsettings.Development.json` |
| Mailhog | SMTP on port 1025; web UI on 8025 |
| Bootstrap SuperAdmin | `IDENTITY_BOOTSTRAP_EMAIL` / `IDENTITY_BOOTSTRAP_PASSWORD` env vars |

---

## Requirements traceability

| RF | Design section | Status |
|----|---------------|--------|
| RF01 JWT auth | §1, §3.1 | Designed |
| RF02 SuperAdmin | §2 seed, §4 bypass | Designed |
| RF03 Email invitation | §3.4 | Designed |
| RF04 Tenant inherit on invite | §3.4 | Designed |
| RF05 SuperAdmin tenant select | §3.4 | Designed |
| RF06 Activation flow | §3.4 | Designed |
| RF07 Forgot password | §3.5 | Designed |
| RF08 Forced reset | §3.5 | Designed |
| RF09 CRUD + tenant isolation | §4, §2 filter | Designed |
| RF10 UI string permissions | §4, §6 | Designed |
| RF11 Multi-role additive | §4 union | Designed |
| RF12 Conflict warnings | §3.7 | Designed |
| RF13 Role delete + migration | §3.7 | Designed |
| RF14 Session eviction | §3.6 | Designed |

All 35 functional criteria (CF-A1 through CF-I5) are addressed by the flows, model, and tests defined above.
