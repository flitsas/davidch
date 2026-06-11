#!/usr/bin/env python3
"""Generate FLIT Identity Postman collection and environment files."""

import json
from pathlib import Path

OUT_DIR = Path(__file__).parent
BASE = "{{baseUrl}}"

COLLECTION_DESC = """# FLIT Identity & RBAC API

Complete API reference for the FLIT modular monolith identity layer (.NET 9).

## Architecture

| Component | Dev URL | Notes |
|-----------|---------|-------|
| **Gateway (YARP)** | `http://localhost:5023` | Unified entry — proxies `/api/*` to backend |
| **Identity API** | `http://localhost:5080` | Direct backend access (bypass gateway) |
| **Frontend** | `http://localhost:3000` | Next.js App Router |
| **Mailhog** | `http://localhost:8025` | Dev email UI (invites, resets) |
| **PostgreSQL** | `localhost:5432` | Database `flit_identity` |

## Authentication Model

FLIT uses **HttpOnly cookie-based JWT authentication** — not Bearer tokens in headers.

| Cookie | Purpose | Lifetime |
|--------|---------|----------|
| `flit_access` | RS256 JWT access token | 15 minutes |
| `flit_refresh` | Opaque refresh token (SHA-256 hashed in DB) | 7 days |

**Cookie flags (Development):** `HttpOnly`, `SameSite=Strict`, `Secure=false` (localhost)

### JWT Claims

| Claim | Description |
|-------|-------------|
| `sub` | User ID (UUID) |
| `email` | Normalized email |
| `tenant_id` | Tenant UUID (null for SuperAdmin) |
| `roles` | Role names (multi-valued) |
| `permissions` | `key\\|scope` pairs (e.g. `users:read\\|Tenant`) |
| `is_super_admin` | `true` for platform SuperAdmin |
| `token_version` | Integer — incremented on session revocation |

### Session Eviction

When privileges change (role assignment, permission update, block, password reset), `token_version` increments. Existing JWTs return `403 SESSION_REVOKED` with header `X-Session-Revoked: true`.

## Authorization (RBAC + ABAC Scopes)

Permissions follow `module:action` format. Each grant has a **scope**:

| Scope | Meaning |
|-------|---------|
| `Global` | All resources across all tenants (SuperAdmin) |
| `Tenant` | Resources within the caller's tenant |
| `Own` | Only resources owned by the caller |

SuperAdmin bypasses permission checks entirely.

## Rate Limiting

| Endpoint | Limit |
|----------|-------|
| `POST /api/auth/login` | Per IP + email |
| `POST /api/auth/forgot-password` | Per IP + email |

Returns `429` with `{ "code": "RATE_LIMITED" }`.

## Bootstrap Credentials (Development)

| Field | Value |
|-------|-------|
| Email | `super@flit.local` |
| Password | `FlitDev2026!` |

## Recommended Flow

1. **Login** → cookies set automatically in Postman
2. **Get Current User** → verify session and permissions
3. Use protected endpoints (Users, Roles, Permissions)
4. **Refresh** when access token expires (or rely on frontend middleware)
5. **Logout** → revokes refresh token, clears cookies

## Error Codes

| Code | HTTP | Meaning |
|------|------|---------|
| `INVALID_CREDENTIALS` | 401 | Wrong email/password or inactive user |
| `TOKEN_EXPIRED` | 401 | Missing/invalid/expired access or refresh token |
| `SESSION_REVOKED` | 403 | `token_version` mismatch — re-login required |
| `FORBIDDEN` | 403 | Authenticated but insufficient permission |
| `VALIDATION_ERROR` | 400 | Invalid request body |
| `NOT_FOUND` | 404 | Resource not found |
| `RATE_LIMITED` | 429 | Too many login/forgot-password attempts |
| `ROLE_HAS_USERS` | 409 | Cannot delete role with assigned users |
| `INVITATION_TOKEN_INVALID` | 400 | Expired/used/invalid activation token |
| `RESET_TOKEN_INVALID` | 400 | Expired/used/invalid password reset token |
| `EMAIL_EXISTS` | 409 | Email already registered |
"""


def req(name, method, path, description, body=None, auth_inherit=True, examples=None):
    """Build a Postman request item."""
    url = f"{BASE}{path}" if path.startswith("/") else path
    item = {
        "name": name,
        "request": {
            "method": method,
            "header": [
                {"key": "Content-Type", "value": "application/json"},
                {"key": "Accept", "value": "application/json"},
            ],
            "url": url,
            "description": description,
        },
    }
    if body:
        item["request"]["body"] = {
            "mode": "raw",
            "raw": json.dumps(body, indent=2),
            "options": {"raw": {"language": "json"}},
        }
    if not auth_inherit:
        item["request"]["auth"] = {"type": "noauth"}
    if examples:
        item["response"] = examples
    return item


STATUS_TEXT = {
    200: "OK",
    201: "Created",
    204: "No Content",
    400: "Bad Request",
    401: "Unauthorized",
    403: "Forbidden",
    404: "Not Found",
    409: "Conflict",
    429: "Too Many Requests",
}


def example(name, status, body=None, *, method="GET", path="/api/health", headers=None):
    h = [{"key": "Content-Type", "value": "application/json"}]
    if headers:
        h.extend(headers)
    url = f"{BASE}{path}" if path.startswith("/") else path
    has_body = body is not None
    return {
        "name": name,
        "originalRequest": {
            "method": method,
            "header": [{"key": "Content-Type", "value": "application/json"}],
            "url": url,
        },
        "status": STATUS_TEXT.get(status, "Error" if status >= 400 else "OK"),
        "code": status,
        "_postman_previewlanguage": "json" if has_body else "plain",
        "header": h,
        "body": json.dumps(body, indent=2) if has_body else "",
    }


def folder(name, description, items):
    return {"name": name, "description": description, "item": items}


# --- Request definitions ---

health = req(
    "Health Check",
    "GET",
    "/api/health",
    """## Overview
Liveness probe for the Identity API. No authentication required.

## Use Cases
- Docker/Kubernetes health checks
- CI pipeline smoke tests
- Verify API is running before login

## Response
`200 OK` with `{ "status": "ok" }`
""",
    auth_inherit=False,
    examples=[example("200 OK", 200, {"status": "ok"}, method="GET", path="/api/health")],
)

login = req(
    "Login",
    "POST",
    "/api/auth/login",
    """## Overview
Authenticates a user with email and password. On success, sets two HttpOnly cookies.

## Use Cases
- User sign-in from web or mobile client
- Obtain session for subsequent API calls
- Admin console access

## Request Body
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `email` | string | Yes | User email (case-insensitive) |
| `password` | string | Yes | Plain-text password (Argon2id verified server-side) |

## Success Response — `200 OK`
```json
{
  "id": "uuid",
  "email": "super@flit.local",
  "tenant_id": null,
  "roles": ["SuperAdmin"]
}
```

**Set-Cookie headers:**
- `flit_access` — JWT (15 min)
- `flit_refresh` — opaque token (7 days)

## Error Responses
| Status | Code | When |
|--------|------|------|
| 401 | `INVALID_CREDENTIALS` | Wrong password, inactive user, or email not found |
| 429 | `RATE_LIMITED` | Too many attempts (per IP + email) |

## Postman Notes
Postman automatically stores cookies after login. Run **Get Current User** to verify.

## Security
- Passwords hashed with Argon2id
- Failed logins audited
- Rate limited per IP and email
""",
    {"email": "{{bootstrapEmail}}", "password": "{{bootstrapPassword}}"},
    auth_inherit=False,
    examples=[
        example("200 SuperAdmin", 200, {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "email": "super@flit.local",
            "tenant_id": None,
            "roles": ["SuperAdmin"],
        }, method="POST", path="/api/auth/login"),
        example("401 Invalid Credentials", 401, {"code": "INVALID_CREDENTIALS"}, method="POST", path="/api/auth/login"),
        example("429 Rate Limited", 429, {"code": "RATE_LIMITED"}, method="POST", path="/api/auth/login"),
    ],
)

refresh = req(
    "Refresh Session",
    "POST",
    "/api/auth/refresh",
    """## Overview
Rotates the refresh token and issues a new access JWT. Requires the `flit_refresh` cookie.

## Use Cases
- Silent token renewal (frontend middleware does this automatically)
- Extend session without re-entering credentials
- Recover from expired access token

## Request
No body. Sends `flit_refresh` cookie automatically.

## Token Rotation
The old refresh token is **revoked** and a new one issued (one-time use). Prevents refresh token replay attacks.

## Success — `200 OK`
Same shape as login response. New cookies set.

## Errors
| Status | Code | When |
|--------|------|------|
| 401 | `TOKEN_EXPIRED` | Missing, revoked, or expired refresh token |
| 403 | `SESSION_REVOKED` | User blocked or token_version changed |
""",
    examples=[
        example("200 OK", 200, {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "email": "super@flit.local",
            "tenant_id": None,
            "roles": ["SuperAdmin"],
        }, method="POST", path="/api/auth/refresh"),
        example("401 Missing Refresh Token", 401, {"code": "TOKEN_EXPIRED"}, method="POST", path="/api/auth/refresh"),
        example("403 Session Revoked", 403, {"code": "SESSION_REVOKED"}, method="POST", path="/api/auth/refresh"),
    ],
)

logout = req(
    "Logout",
    "POST",
    "/api/auth/logout",
    """## Overview
Revokes the current refresh token and clears both auth cookies.

## Use Cases
- User-initiated sign out
- Security: invalidate session on shared device
- E2E test teardown

## Request
No body. Uses cookies if present.

## Success — `204 No Content`
Cookies deleted via `Set-Cookie` with expired date.

## Notes
Idempotent — succeeds even when no refresh cookie is present.
""",
    examples=[
        example("204 No Content", 204, method="POST", path="/api/auth/logout"),
    ],
)

me = req(
    "Get Current User",
    "GET",
    "/api/auth/me",
    """## Overview
Returns the authenticated user's profile, roles, and resolved permissions from the JWT.

## Use Cases
- Frontend session bootstrap (`/api/auth/me` on page load)
- Permission guard checks in UI
- Verify login succeeded in Postman

## Authentication
Requires valid `flit_access` cookie (or expired → call Refresh first).

## Success — `200 OK`
```json
{
  "id": "uuid",
  "email": "user@example.com",
  "tenantId": "uuid-or-null",
  "roles": ["Admin"],
  "permissions": [
    { "key": "users:read", "scope": "Tenant" }
  ],
  "isSuperAdmin": false
}
```

## Errors
| Status | Code | Header | When |
|--------|------|--------|------|
| 401 | `TOKEN_EXPIRED` | — | No/invalid/expired JWT |
| 403 | `SESSION_REVOKED` | `X-Session-Revoked: true` | token_version mismatch |
""",
    examples=[
        example("200 SuperAdmin", 200, {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "email": "super@flit.local",
            "tenantId": None,
            "roles": ["SuperAdmin"],
            "permissions": [{"key": "users:read", "scope": "Global"}],
            "isSuperAdmin": True,
        }, method="GET", path="/api/auth/me"),
        example("401 Token Expired", 401, {"code": "TOKEN_EXPIRED"}, method="GET", path="/api/auth/me"),
        example(
            "403 Session Revoked",
            403,
            {"code": "SESSION_REVOKED", "message": "Session revoked."},
            method="GET",
            path="/api/auth/me",
            headers=[{"key": "X-Session-Revoked", "value": "true"}],
        ),
    ],
)

activate = req(
    "Activate Account",
    "POST",
    "/api/auth/activate",
    """## Overview
Completes user onboarding by setting a password from an invitation link.

## Use Cases
- New employee accepts invite email
- First-time password setup for invited users

## Flow
1. Admin calls **Invite User** → email sent with `/activate?token=...`
2. User opens link, submits password
3. User status changes `Pending` → `Active`

## Request Body
| Field | Type | Required |
|-------|------|----------|
| `token` | string | Yes — from invitation email URL |
| `password` | string | Yes — new password (Argon2id hashed) |

## Success — `200 OK`
`{ "email": "newuser@tenant.com" }`

## Errors
| Status | Code | When |
|--------|------|------|
| 400 | `VALIDATION_ERROR` | Missing token/password |
| 400 | `INVITATION_TOKEN_INVALID` | Expired (72h), used, or invalid token |

## Token Source
Check Mailhog at `http://localhost:8025` for invitation emails in development.
""",
    {"token": "{{activationToken}}", "password": "SecurePass!123"},
    auth_inherit=False,
    examples=[
        example("200 OK", 200, {"email": "newuser@tenant.com"}, method="POST", path="/api/auth/activate"),
        example(
            "400 Validation Error",
            400,
            {"code": "VALIDATION_ERROR", "message": "Token and password are required."},
            method="POST",
            path="/api/auth/activate",
        ),
        example("400 Invalid Token", 400, {"code": "INVITATION_TOKEN_INVALID"}, method="POST", path="/api/auth/activate"),
    ],
)

forgot = req(
    "Forgot Password",
    "POST",
    "/api/auth/forgot-password",
    """## Overview
Initiates self-service password reset. Always returns the same message (no email enumeration).

## Use Cases
- User forgot password on login page
- Self-service account recovery

## Request Body
| Field | Type | Required |
|-------|------|----------|
| `email` | string | Yes |

## Success — `200 OK` (always)
```json
{ "message": "If an account exists, a reset link has been sent." }
```

## Side Effects (if account exists)
- Creates `PasswordResetToken` (1 hour expiry, type `SelfService`)
- Sends email with `/reset-password?token=...` link

## Rate Limited
Returns `429 RATE_LIMITED` on abuse (per IP + email).

## Security
Response is identical whether email exists — prevents account enumeration.
""",
    {"email": "user@tenant.com"},
    auth_inherit=False,
    examples=[
        example(
            "200 OK",
            200,
            {"message": "If an account exists, a reset link has been sent."},
            method="POST",
            path="/api/auth/forgot-password",
        ),
        example("429 Rate Limited", 429, {"code": "RATE_LIMITED"}, method="POST", path="/api/auth/forgot-password"),
    ],
)

reset = req(
    "Reset Password",
    "POST",
    "/api/auth/reset-password",
    """## Overview
Sets a new password using a reset token from email (self-service or admin-forced).

## Use Cases
- Complete forgot-password flow
- Admin forced reset (user receives same email format)

## Request Body
| Field | Type | Required |
|-------|------|----------|
| `token` | string | Yes — from reset email URL |
| `new_password` | string | Yes |

## Success — `200 OK`
`{ "email": "user@tenant.com" }`

## Side Effects
- All sessions revoked (`token_version` incremented)
- Reset token marked used

## Errors
| Status | Code | When |
|--------|------|------|
| 400 | `VALIDATION_ERROR` | Missing fields |
| 400 | `RESET_TOKEN_INVALID` | Expired or already used |
""",
    {"token": "{{resetToken}}", "new_password": "NewSecurePass!456"},
    auth_inherit=False,
    examples=[
        example("200 OK", 200, {"email": "user@tenant.com"}, method="POST", path="/api/auth/reset-password"),
        example(
            "400 Validation Error",
            400,
            {"code": "VALIDATION_ERROR", "message": "Token and new password are required."},
            method="POST",
            path="/api/auth/reset-password",
        ),
        example("400 Invalid Token", 400, {"code": "RESET_TOKEN_INVALID"}, method="POST", path="/api/auth/reset-password"),
    ],
)

permissions = req(
    "List Permissions",
    "GET",
    "/api/permissions",
    """## Overview
Returns the full RBAC permission catalog for building role editors.

## Required Permission
`roles:read` with scope **Tenant** (or SuperAdmin bypass).

## Use Cases
- Admin console role permission picker
- Audit available capabilities
- API integration for custom admin tools

## Success — `200 OK`
Array of permission objects:
```json
[
  {
    "id": "uuid",
    "key": "users:read",
    "type": "Crud",
    "module": "users",
    "description": "users read"
  }
]
```

## Seeded Permissions
Modules: `users`, `roles`, `tramites` — each with `create`, `read`, `update`, `delete`.
Plus UI permission: `generar_consolidado`.
""",
    examples=[example("200 OK", 200, [
        {"id": "a1", "key": "users:read", "type": "Crud", "module": "users", "description": "users read"},
        {"id": "a2", "key": "roles:create", "type": "Crud", "module": "roles", "description": "roles create"},
    ], method="GET", path="/api/permissions")],
)

list_roles = req(
    "List Roles",
    "GET",
    "/api/roles",
    """## Overview
Lists all roles visible in the current tenant context (EF global query filter).

## Required Permission
`roles:read` (Tenant scope).

## Use Cases
- Admin role management table
- Dropdown for user role assignment

## Success — `200 OK`
```json
[
  {
    "id": "uuid",
    "name": "TenantAdmin",
    "isSystem": false,
    "tenantId": "uuid",
    "createdAt": "2026-06-10T00:00:00Z"
  }
]
```

## Notes
- SuperAdmin role has `tenantId: null`, `isSystem: true`
- Tenant-scoped users only see their tenant's roles
""",
    examples=[example("200 OK", 200, [{
        "id": "r1", "name": "TenantAdmin", "isSystem": False,
        "tenantId": "t1", "createdAt": "2026-06-10T00:00:00+00:00",
    }], method="GET", path="/api/roles")],
)

create_role = req(
    "Create Role",
    "POST",
    "/api/roles",
    """## Overview
Creates a new tenant-scoped custom role.

## Required Permission
`roles:create` (Tenant scope).

## Request Body
| Field | Type | Required |
|-------|------|----------|
| `name` | string | Yes — unique within tenant |

## Success — `201 Created`
Role summary + `Location: /api/roles/{id}` header.

## Errors
| Status | Code | When |
|--------|------|------|
| 400 | `VALIDATION_ERROR` | Empty name or missing tenant context |
""",
    {"name": "ClaimsReviewer"},
    examples=[example("201 Created", 201, {
        "id": "r-new", "name": "ClaimsReviewer", "isSystem": False,
        "tenantId": "t1", "createdAt": "2026-06-10T00:00:00+00:00",
    }, method="POST", path="/api/roles")],
)

get_role = req(
    "Get Role",
    "GET",
    "/api/roles/{{roleId}}",
    """## Overview
Returns role details including assigned permissions and scopes.

## Required Permission
`roles:read` (Tenant scope).

## Path Parameters
| Param | Description |
|-------|-------------|
| `id` | Role UUID |

## Success — `200 OK`
```json
{
  "id": "uuid",
  "name": "ClaimsReviewer",
  "isSystem": false,
  "tenantId": "uuid",
  "createdAt": "...",
  "permissions": [
    { "permissionId": "uuid", "key": "tramites:read", "scope": "Tenant" }
  ]
}
```
""",
    examples=[example("200 OK", 200, {
        "id": "r1", "name": "ClaimsReviewer", "isSystem": False,
        "tenantId": "t1", "createdAt": "2026-06-10T00:00:00+00:00",
        "permissions": [{"permissionId": "p1", "key": "tramites:read", "scope": "Tenant"}],
    }, method="GET", path="/api/roles/{{roleId}}")],
)

update_role = req(
    "Update Role",
    "PUT",
    "/api/roles/{{roleId}}",
    """## Overview
Renames a custom (non-system) role.

## Required Permission
`roles:update` (Tenant scope).

## Request Body
| Field | Type | Required |
|-------|------|----------|
| `name` | string | Yes |

## Success — `200 OK`
Updated role summary.

## Errors
| Status | Code | When |
|--------|------|------|
| 403 | `FORBIDDEN` | Attempting to modify system role (e.g. SuperAdmin) |
| 404 | `NOT_FOUND` | Role doesn't exist |
""",
    {"name": "Senior Claims Reviewer"},
    examples=[example("200 OK", 200, {
        "id": "r1", "name": "Senior Claims Reviewer", "isSystem": False,
        "tenantId": "t1", "createdAt": "2026-06-10T00:00:00+00:00",
    }, method="PUT", path="/api/roles/{{roleId}}")],
)

set_role_perms = req(
    "Set Role Permissions",
    "PUT",
    "/api/roles/{{roleId}}/permissions",
    """## Overview
Replaces all permissions on a role. **Revokes sessions** for all users with this role.

## Required Permission
`roles:update` (Tenant scope).

## Request Body
```json
{
  "permissions": [
    { "permission_id": "uuid", "scope": "Tenant" }
  ]
}
```

## Scope Values
`Global`, `Tenant`, `Own`

## Success — `204 No Content`

## Side Effects
- All users with this role get `token_version` incremented
- Audit log: `role_permissions_changed`

## Errors
| Status | Code | When |
|--------|------|------|
| 400 | `VALIDATION_ERROR` | Invalid permission IDs |
| 403 | `FORBIDDEN` | System role modification |
""",
    {"permissions": [{"permission_id": "{{permissionId}}", "scope": "Tenant"}]},
    examples=[example("204 No Content", 204, method="PUT", path="/api/roles/{{roleId}}/permissions")],
)

delete_role = req(
    "Delete Role",
    "DELETE",
    "/api/roles/{{roleId}}",
    """## Overview
Permanently deletes an empty custom role.

## Required Permission
`roles:delete` (Tenant scope).

## Success — `204 No Content`

## Errors
| Status | Code | When |
|--------|------|------|
| 403 | `FORBIDDEN` | System role |
| 404 | `NOT_FOUND` | Role not found |
| 409 | `ROLE_HAS_USERS` | Role still assigned — use **Migrate Role** first |

## Response (409)
```json
{ "code": "ROLE_HAS_USERS", "affected_users": 5 }
```
""",
    examples=[
        example("204 No Content", 204, method="DELETE", path="/api/roles/{{roleId}}"),
        example("409 Role Has Users", 409, {"code": "ROLE_HAS_USERS", "affected_users": 5}, method="DELETE", path="/api/roles/{{roleId}}"),
    ],
)

migrate_role = req(
    "Migrate Role",
    "POST",
    "/api/roles/{{roleId}}/migrate",
    """## Overview
Moves all users from a deprecated role to a replacement role, then deletes the source role.

## Required Permission
`roles:delete` (Tenant scope).

## Use Cases
- Deprecate old role names without orphaning users
- RBAC restructuring

## Request Body
```json
{ "replacement_role_id": "uuid" }
```

## Validation
- Source ≠ replacement
- Both roles same tenant
- Cannot migrate to/from system roles

## Success — `204 No Content`

## Side Effects
- Users reassigned (deduplicated if already have replacement role)
- Source role and its permissions deleted
- All affected users' sessions revoked
- Audit log: `role_migrated`
""",
    {"replacement_role_id": "{{replacementRoleId}}"},
    examples=[example("204 No Content", 204, method="POST", path="/api/roles/{{roleId}}/migrate")],
)

invite = req(
    "Invite User",
    "POST",
    "/api/users/invite",
    """## Overview
Creates a pending user, assigns roles, and sends activation email.

## Required Permission
`users:create` (Tenant scope).

## Request Body
| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `email` | string | Yes | Normalized to lowercase |
| `role_ids` | uuid[] | No | Must belong to target tenant |
| `tenant_id` | uuid | SuperAdmin only | Required when inviter is SuperAdmin |

## Tenant Inheritance
- **Tenant admin:** `tenant_id` inherited from JWT (ignored in body)
- **SuperAdmin:** must specify `tenant_id`

## Success — `201 Created`
```json
{
  "user_id": "uuid",
  "email": "new@tenant.com",
  "status": "Pending"
}
```

## Side Effects
- Invitation token created (72h expiry)
- Email sent via SMTP (Mailhog in dev)

## Errors
| Status | Code | When |
|--------|------|------|
| 400 | `VALIDATION_ERROR` | Invalid roles or missing tenant_id |
| 409 | `EMAIL_EXISTS` | Email already registered |
""",
    {"email": "newuser@tenant.com", "role_ids": ["{{roleId}}"], "tenant_id": "{{tenantId}}"},
    examples=[example("201 Created", 201, {
        "user_id": "u-new", "email": "newuser@tenant.com", "status": "Pending",
    }, method="POST", path="/api/users/invite")],
)

list_users = req(
    "List Users",
    "GET",
    "/api/users",
    """## Overview
Lists users in the current tenant (filtered by EF global query filter).

## Required Permission
`users:read` (Tenant scope).

## Success — `200 OK`
```json
[
  {
    "id": "uuid",
    "email": "user@tenant.com",
    "status": "Active",
    "tenantId": "uuid",
    "createdAt": "...",
    "activatedAt": "..."
  }
]
```

## Status Values
`Pending`, `Active`, `Blocked`

## SuperAdmin
Sees all users (no tenant filter when `tenant_id` claim is null).
""",
    examples=[example("200 OK", 200, [{
        "id": "u1", "email": "user@tenant.com", "status": "Active",
        "tenantId": "t1", "createdAt": "2026-06-01T00:00:00+00:00",
        "activatedAt": "2026-06-02T00:00:00+00:00",
    }], method="GET", path="/api/users")],
)

set_user_roles = req(
    "Set User Roles",
    "PUT",
    "/api/users/{{userId}}/roles",
    """## Overview
Replaces all roles assigned to a user. Supports conflict detection with confirmation.

## Required Permission
`users:update` (Tenant scope).

## Request Body
```json
{
  "role_ids": ["uuid1", "uuid2"],
  "confirm": false
}
```

## Two-Phase Flow
1. **First call** (`confirm: false`) — if role conflicts detected, returns `200` with warnings:
```json
{
  "pending": true,
  "warnings": [
    {
      "type": "SCOPE_SUBSUMPTION",
      "message": "...",
      "role_id": "uuid",
      "permission_key": "users:read"
    }
  ]
}
```
2. **Confirm** (`confirm: true`) — applies changes despite warnings.

## Success — `204 No Content`

## Side Effects
- User sessions revoked immediately
- Audit log: `user_roles_changed`

## Errors
| Status | Code | When |
|--------|------|------|
| 400 | `VALIDATION_ERROR` | Empty role_ids or invalid IDs |
| 403 | `FORBIDDEN` | Cross-tenant assignment |
| 404 | `NOT_FOUND` | User not found |
""",
    {"role_ids": ["{{roleId}}"], "confirm": False},
    examples=[
        example(
            "200 Pending Confirmation",
            200,
            {
                "pending": True,
                "warnings": [{
                    "type": "SCOPE_SUBSUMPTION",
                    "message": "Role Admin already grants users:read at Tenant scope.",
                    "role_id": "r1",
                    "permission_key": "users:read",
                }],
            },
            method="PUT",
            path="/api/users/{{userId}}/roles",
        ),
        example("204 Applied", 204, method="PUT", path="/api/users/{{userId}}/roles"),
    ],
)

force_reset = req(
    "Force Password Reset",
    "POST",
    "/api/users/{{userId}}/force-reset",
    """## Overview
Admin-initiated password reset. Revokes all sessions and emails a reset link.

## Required Permission
`users:update` (Tenant scope).

## Use Cases
- Suspected account compromise
- Employee offboarding preparation
- Compliance-mandated credential rotation

## Success — `200 OK`
```json
{ "user_id": "uuid", "email": "user@tenant.com" }
```

## Side Effects
- All sessions revoked immediately
- `PasswordResetToken` created (24h expiry, type `Forced`)
- Reset email sent
- Audit log: `user_force_reset`

## Errors
| Status | Code | When |
|--------|------|------|
| 400 | `VALIDATION_ERROR` | User not Active |
| 404 | `NOT_FOUND` | User not found |
""",
    examples=[example("200 OK", 200, {"user_id": "u1", "email": "user@tenant.com"}, method="POST", path="/api/users/{{userId}}/force-reset")],
)

block_user = req(
    "Block User",
    "POST",
    "/api/users/{{userId}}/block",
    """## Overview
Blocks a user account and revokes all active sessions.

## Required Permission
`users:update` (Tenant scope).

## Use Cases
- Terminate access for departing employee
- Security incident response
- Suspend abusive account

## Success — `204 No Content`
Idempotent if user already blocked.

## Side Effects
- Status → `Blocked`
- All sessions revoked
- Audit log: `user_blocked`

## Errors
| Status | Code | When |
|--------|------|------|
| 403 | `FORBIDDEN` | Cross-tenant block attempt |
| 404 | `NOT_FOUND` | User not found |
""",
    examples=[example("204 No Content", 204, method="POST", path="/api/users/{{userId}}/block")],
)

list_tenants = req(
    "List Tenants",
    "GET",
    "/api/tenants",
    """## Overview
Lists active tenants. SuperAdmin-only endpoint.

## Required Access
Authenticated SuperAdmin (`is_super_admin: true` in JWT).

## Use Cases
- SuperAdmin tenant picker in admin console
- Resolve `tenant_id` before inviting users to a tenant

## Success — `200 OK`
```json
[
  { "id": "uuid", "name": "Acme Corp", "slug": "acme" }
]
```

## Errors
| Status | Code | When |
|--------|------|------|
| 401 | `TOKEN_EXPIRED` | Not authenticated |
| 403 | `FORBIDDEN` | Authenticated but not SuperAdmin |
""",
    examples=[example("200 OK", 200, [
        {"id": "t1", "name": "Acme Corp", "slug": "acme"},
        {"id": "t2", "name": "Globex", "slug": "globex"},
    ], method="GET", path="/api/tenants")],
)

collection = {
    "info": {
        "name": "FLIT Identity & RBAC API",
        "description": COLLECTION_DESC,
        "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
    },
    "variable": [
        {"key": "baseUrl", "value": "http://localhost:5080", "description": "Identity API direct (use http://localhost:5023 for gateway)"},
        {"key": "bootstrapEmail", "value": "super@flit.local"},
        {"key": "bootstrapPassword", "value": "FlitDev2026!"},
        {"key": "userId", "value": "", "description": "Target user UUID"},
        {"key": "roleId", "value": "", "description": "Target role UUID"},
        {"key": "replacementRoleId", "value": "", "description": "Replacement role for migration"},
        {"key": "permissionId", "value": "", "description": "Permission UUID from List Permissions"},
        {"key": "tenantId", "value": "", "description": "Tenant UUID (SuperAdmin invites)"},
        {"key": "activationToken", "value": "", "description": "From invitation email /activate?token="},
        {"key": "resetToken", "value": "", "description": "From reset email /reset-password?token="},
    ],
    "event": [
        {
            "listen": "prerequest",
            "script": {
                "type": "text/javascript",
                "exec": [
                    "// Collection pre-request: ensure cookies are sent",
                    "// Postman sends cookies automatically for matching domains",
                ],
            },
        },
        {
            "listen": "test",
            "script": {
                "type": "text/javascript",
                "exec": [
                    "// Auto-save IDs from responses for chained requests",
                    "if (pm.response.code === 200 || pm.response.code === 201) {",
                    "  try {",
                    "    const json = pm.response.json();",
                    "    if (json.id) pm.collectionVariables.set('lastId', json.id);",
                    "    if (json.user_id) pm.collectionVariables.set('userId', json.user_id);",
                    "    if (Array.isArray(json) && json[0]?.id) pm.collectionVariables.set('roleId', json[0].id);",
                    "  } catch (e) {}",
                    "}",
                ],
            },
        },
    ],
    "item": [
        folder("Health", "Infrastructure endpoints", [health]),
        folder(
            "Authentication",
            "Cookie-based JWT auth. Run **Login** first — Postman stores `flit_access` and `flit_refresh` cookies automatically.",
            [
                folder(
                    "Session",
                    "Sign-in, token refresh, logout, and current user profile.",
                    [login, refresh, logout, me],
                ),
                folder(
                    "Account Activation",
                    "Complete invited-user onboarding with an activation token.",
                    [activate],
                ),
                folder(
                    "Password Recovery",
                    "Self-service forgot-password and reset-password flows.",
                    [forgot, reset],
                ),
            ],
        ),
        folder(
            "RBAC — Permissions",
            "Permission catalog for role configuration.",
            [permissions],
        ),
        folder(
            "RBAC — Roles",
            "Tenant-scoped role CRUD, permission assignment, and migration.",
            [list_roles, create_role, get_role, update_role, set_role_perms, delete_role, migrate_role],
        ),
        folder(
            "Users",
            "User lifecycle: invite, list, assign roles, force reset, block.",
            [invite, list_users, set_user_roles, force_reset, block_user],
        ),
        folder(
            "Tenants",
            "Platform tenant catalog (SuperAdmin only).",
            [list_tenants],
        ),
    ],
}

environment = {
    "name": "FLIT Identity — Local Development",
    "values": [
        {"key": "baseUrl", "value": "http://localhost:5080", "type": "default", "enabled": True, "description": "Direct API. Use http://localhost:5023 for YARP gateway."},
        {"key": "gatewayUrl", "value": "http://localhost:5023", "type": "default", "enabled": True},
        {"key": "bootstrapEmail", "value": "super@flit.local", "type": "default", "enabled": True},
        {"key": "bootstrapPassword", "value": "ChangeMe!123", "type": "secret", "enabled": True},
        {"key": "mailhogUrl", "value": "http://localhost:8025", "type": "default", "enabled": True},
        {"key": "userId", "value": "", "type": "default", "enabled": True},
        {"key": "roleId", "value": "", "type": "default", "enabled": True},
        {"key": "tenantId", "value": "", "type": "default", "enabled": True},
        {"key": "permissionId", "value": "", "type": "default", "enabled": True},
        {"key": "activationToken", "value": "", "type": "default", "enabled": True},
        {"key": "resetToken", "value": "", "type": "default", "enabled": True},
    ],
}

if __name__ == "__main__":
    coll_path = OUT_DIR / "FLIT-Identity-API.postman_collection.json"
    env_path = OUT_DIR / "FLIT-Identity-Local.postman_environment.json"

    with open(coll_path, "w", encoding="utf-8") as f:
        json.dump(collection, f, indent=2, ensure_ascii=False)

    with open(env_path, "w", encoding="utf-8") as f:
        json.dump(environment, f, indent=2, ensure_ascii=False)

    print(f"Wrote {coll_path}")
    print(f"Wrote {env_path}")
