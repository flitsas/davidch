# FLIT Identity — Postman Collection

API documentation and runnable requests for the FLIT identity module (22 endpoints).

## Files

| File | Description |
|------|-------------|
| `FLIT-Identity-API.postman_collection.json` | **Recommended import** — folders, full markdown docs, example responses, collection scripts |
| `FLIT-Identity-Local.postman_environment.json` | Local dev variables (base URL, bootstrap credentials, tokens) |
| `generate-collection.py` | Regenerate collection/env from source when endpoints change |

## Import into Postman

1. **Postman** → **Import** → select both JSON files
2. Select environment **FLIT Identity — Local Development**
3. Run **Authentication → Session → Login** first (cookies are stored automatically)
4. Run **Get Current User** to verify session

## Postman Cloud (synced)

Also published to workspace **David Alejandro Chica Hernández's Workspace**:

- Collection: **FLIT Identity & RBAC API** (`55463689-54f43d59-cfe4-446d-bf64-b79a6e5c59a8`)
- Environment: **FLIT Identity — Local Development**

Re-import the local JSON file above to get the latest folder layout, docs, and saved examples.

## Quick start flow

```
Health Check → Authentication → Session → Login → Get Current User → (protected endpoints)
```

## Base URLs

| Target | URL |
|--------|-----|
| API direct | `http://localhost:5080` |
| YARP gateway | `http://localhost:5023` |

Set `baseUrl` in the environment to switch.

## Bootstrap credentials (dev)

- Email: `super@flit.local`
- Password: `FlitDev2026!`

## Collection folders

| Folder | Endpoints |
|--------|-----------|
| **Health** | Liveness probe |
| **Authentication → Session** | Login, Refresh, Logout, Get Current User |
| **Authentication → Account Activation** | Activate Account |
| **Authentication → Password Recovery** | Forgot Password, Reset Password |
| **RBAC — Permissions** | List Permissions |
| **RBAC — Roles** | Role CRUD, permissions, migration |
| **Users** | Invite, list, roles, force reset, block |
| **Tenants** | List Tenants (SuperAdmin) |

## Endpoint index (22)

### Health
- `GET /api/health`

### Authentication (cookie-based JWT)
- `POST /api/auth/login` — rate limited
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`
- `POST /api/auth/activate`
- `POST /api/auth/forgot-password` — rate limited
- `POST /api/auth/reset-password`

### RBAC
- `GET /api/permissions`
- `GET /api/roles` · `POST /api/roles`
- `GET /api/roles/{id}` · `PUT /api/roles/{id}` · `DELETE /api/roles/{id}`
- `PUT /api/roles/{id}/permissions`
- `POST /api/roles/{id}/migrate`

### Users
- `POST /api/users/invite` · `GET /api/users`
- `PUT /api/users/{id}/roles`
- `POST /api/users/{id}/force-reset`
- `POST /api/users/{id}/block`

### Tenants
- `GET /api/tenants` — SuperAdmin only

## Regenerate after API changes

```bash
python3 docs/postman/generate-collection.py
```

Then re-import or sync to Postman.
