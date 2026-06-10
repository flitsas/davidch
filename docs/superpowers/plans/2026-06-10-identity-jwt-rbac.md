# Identity Layer (JWT, RBAC, Multi-Tenant) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the full FLIT 2.0 identity layer — JWT auth with httpOnly cookies, RBAC + lightweight ABAC scopes, multi-tenant isolation, invitation onboarding, credential lifecycle, and session eviction — as specified in Feature #9548.

**Architecture:** A .NET 9 modular monolith (`Auth`, `Users`, `RBAC`, `Notifications`) backed by PostgreSQL and exposed at `/api/*` through a YARP gateway on a unified domain. Next.js 16 (`/home/david/davidch/frontend`) serves the UI at `/*`, uses middleware for silent refresh, and never reads tokens from JavaScript.

**Tech Stack:** .NET 9, ASP.NET Core Minimal APIs, EF Core 9 + Npgsql, YARP, Argon2id (`Konscious.Security.Cryptography.Argon2`), RS256 JWT (`System.IdentityModel.Tokens.Jwt`), MailKit, PostgreSQL 16, Mailhog, Next.js 16, React 19, TypeScript, Tailwind v4, Playwright (E2E)

**Spec:** `docs/superpowers/specs/2026-06-09-identity-jwt-rbac-design.md`

**ADO Feature:** [#9548](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9548)

| Story | Title | SP |
|-------|-------|-----|
| [#9711](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9711) | Schema PostgreSQL, seed y migraciones EF Core | 5 |
| [#9712](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9712) | JWT login, refresh, logout y cookies httpOnly | 8 |
| [#9713](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9713) | RBAC catálogo, scopes y filtro tenant EF Core | 8 |
| [#9714](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9714) | Invitación por email, activación y herencia tenant | 8 |
| [#9715](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9715) | Olvido de contraseña, reset forzado y SMTP Mailhog | 5 |
| [#9716](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9716) | Desalojo de sesión por token_version | 5 |
| [#9717](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9717) | Conflictos de roles y migración en lote | 5 |
| [#9718](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9718) | Middleware auth, login y flujos de credenciales | 8 |
| [#9719](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9719) | Consola usuarios, roles y PermissionGuard | 8 |
| [#9720](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9720) | E2E seguridad multi-tenant e identidad | 5 |

**Dependency order:** 9711 → 9712/9713 → 9714/9715/9716/9717 → 9718/9719 → 9720

---

## File Map

### Backend (`/home/david/davidch/backend/`)

| File | Responsibility |
|------|----------------|
| `Flit.Identity.sln` | Solution entry |
| `src/Flit.Identity.Api/Program.cs` | Host, middleware pipeline, endpoint registration |
| `src/Flit.Identity.Api/appsettings.Development.json` | Connection strings, JWT keys path, SMTP |
| `src/Flit.Identity.Shared/Domain/UserStatus.cs` | `Pending`, `Active`, `Blocked` enum |
| `src/Flit.Identity.Shared/Domain/PermissionScope.cs` | `Global`, `Tenant`, `Own` enum |
| `src/Flit.Identity.Shared/Domain/PermissionType.cs` | `Crud`, `Ui` enum |
| `src/Flit.Identity.Shared/Domain/ResetTokenType.cs` | `SelfService`, `Forced` enum |
| `src/Flit.Identity.Shared/Auth/PermissionGrant.cs` | `{ Key, Scope }` DTO for JWT |
| `src/Flit.Identity.Shared/Auth/CurrentUser.cs` | Request-scoped user context |
| `src/Flit.Identity.Shared/Errors/ApiErrorCodes.cs` | `INVALID_CREDENTIALS`, `SESSION_REVOKED`, etc. |
| `src/Flit.Identity.Infrastructure/Persistence/IdentityDbContext.cs` | EF Core DbContext + query filters |
| `src/Flit.Identity.Infrastructure/Persistence/Entities/*.cs` | EF entity classes |
| `src/Flit.Identity.Infrastructure/Persistence/Configurations/*.cs` | Fluent API configs |
| `src/Flit.Identity.Infrastructure/Persistence/Seed/IdentityDbSeeder.cs` | SuperAdmin, permissions catalog |
| `src/Flit.Identity.Infrastructure/Persistence/Migrations/*` | EF migrations |
| `src/Flit.Identity.Infrastructure/Tenancy/ITenantContext.cs` | `CurrentTenantId`, `IsSuperAdmin` |
| `src/Flit.Identity.Infrastructure/Tenancy/TenantContext.cs` | Scoped implementation |
| `src/Flit.Identity.Infrastructure/Security/Argon2PasswordHasher.cs` | Hash/verify passwords |
| `src/Flit.Identity.Infrastructure/Security/TokenHasher.cs` | SHA-256 for opaque tokens |
| `src/Flit.Identity.Infrastructure/Security/RsaJwtService.cs` | RS256 sign/validate JWT |
| `src/Flit.Identity.Infrastructure/Security/CookieAuthOptions.cs` | Cookie names, TTLs |
| `src/Flit.Identity.Auth/LoginHandler.cs` | Login flow |
| `src/Flit.Identity.Auth/RefreshHandler.cs` | Refresh rotation |
| `src/Flit.Identity.Auth/LogoutHandler.cs` | Revoke refresh + clear cookies |
| `src/Flit.Identity.Auth/MeHandler.cs` | Current user endpoint |
| `src/Flit.Identity.Auth/ActivateHandler.cs` | Invitation activation |
| `src/Flit.Identity.Auth/ForgotPasswordHandler.cs` | Self-service reset request |
| `src/Flit.Identity.Auth/ResetPasswordHandler.cs` | Complete reset |
| `src/Flit.Identity.Auth/PermissionResolver.cs` | Multi-role additive union |
| `src/Flit.Identity.Auth/SessionRevocationService.cs` | Bump `token_version`, revoke refresh tokens |
| `src/Flit.Identity.Rbac/AuthorizationService.cs` | `CanAccess(user, key, resource?)` |
| `src/Flit.Identity.Rbac/RoleConflictAnalyzer.cs` | Redundancy + scope conflict detection |
| `src/Flit.Identity.Rbac/Endpoints/RolesEndpoints.cs` | CRUD + migrate |
| `src/Flit.Identity.Rbac/Endpoints/PermissionsEndpoints.cs` | Read-only catalog |
| `src/Flit.Identity.Users/Endpoints/UsersEndpoints.cs` | User CRUD, invite, force-reset |
| `src/Flit.Identity.Users/InviteUserHandler.cs` | Invitation creation |
| `src/Flit.Identity.Notifications/EmailSender.cs` | MailKit SMTP wrapper |
| `src/Flit.Identity.Notifications/Templates/*.cshtml` | Invitation, reset emails |
| `tests/Flit.Identity.UnitTests/*` | Unit tests |
| `tests/Flit.Identity.IntegrationTests/*` | WebApplicationFactory + Testcontainers |

### Gateway (`/home/david/davidch/gateway/`)

| File | Responsibility |
|------|----------------|
| `Flit.Gateway/Program.cs` | YARP reverse proxy |
| `Flit.Gateway/appsettings.Development.json` | Routes: `/api/*` → .NET, `/*` → Next.js |

### Docker (`/home/david/davidch/docker/`)

| File | Responsibility |
|------|----------------|
| `docker-compose.yml` | PostgreSQL 16 + Mailhog |
| `.env.example` | Bootstrap credentials, connection strings |

### Frontend (`/home/david/davidch/frontend/`)

| File | Responsibility |
|------|----------------|
| `middleware.ts` | Auth guard, silent refresh, public route allowlist |
| `lib/auth/cookies.ts` | Cookie names/constants |
| `lib/auth/api-client.ts` | Server-side fetch forwarding cookies to `/api/*` |
| `lib/auth/session.ts` | `getSession()`, `requireAuth()` helpers |
| `lib/auth/permissions.ts` | `hasPermission()`, types |
| `components/auth/Can.tsx` | Permission-gated UI wrapper |
| `components/auth/SessionRevokedBanner.tsx` | Alert after eviction |
| `app/login/page.tsx` | Login form |
| `app/activate/page.tsx` | Invitation activation |
| `app/forgot-password/page.tsx` | Forgot password form |
| `app/reset-password/page.tsx` | Reset password form |
| `app/admin/users/page.tsx` | User admin console |
| `app/admin/roles/page.tsx` | Role admin console |
| `app/admin/roles/[id]/page.tsx` | Role detail + permissions |
| `e2e/identity/*.spec.ts` | Playwright E2E suite |

---

## Task 0: Local dev scaffold (prerequisite for all stories)

**Files:**
- Create: `docker/docker-compose.yml`
- Create: `docker/.env.example`
- Create: `backend/Flit.Identity.sln` (+ projects via `dotnet new`)
- Create: `gateway/Flit.Gateway/Program.cs`
- Create: `backend/keys/.gitignore` (ignore `*.pem`)

- [ ] **Step 1: Create docker-compose for PostgreSQL and Mailhog**

```yaml
# docker/docker-compose.yml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: flit
      POSTGRES_PASSWORD: flit_dev
      POSTGRES_DB: flit_identity
    ports:
      - "5432:5432"
    volumes:
      - flit_pg_data:/var/lib/postgresql/data

  mailhog:
    image: mailhog/mailhog:v1.0.1
    ports:
      - "1025:1025"
      - "8025:8025"

volumes:
  flit_pg_data:
```

- [ ] **Step 2: Start infrastructure**

Run: `docker compose -f docker/docker-compose.yml up -d`
Expected: `postgres` and `mailhog` containers running; Mailhog UI at http://localhost:8025

- [ ] **Step 3: Scaffold .NET solution**

```bash
cd /home/david/davidch
mkdir -p backend/src backend/tests backend/keys gateway

dotnet new sln -n Flit.Identity -o backend
dotnet new web -n Flit.Identity.Api -o backend/src/Flit.Identity.Api --no-openapi
dotnet new classlib -n Flit.Identity.Shared -o backend/src/Flit.Identity.Shared
dotnet new classlib -n Flit.Identity.Infrastructure -o backend/src/Flit.Identity.Infrastructure
dotnet new classlib -n Flit.Identity.Auth -o backend/src/Flit.Identity.Auth
dotnet new classlib -n Flit.Identity.Users -o backend/src/Flit.Identity.Users
dotnet new classlib -n Flit.Identity.Rbac -o backend/src/Flit.Identity.Rbac
dotnet new classlib -n Flit.Identity.Notifications -o backend/src/Flit.Identity.Notifications
dotnet new xunit -n Flit.Identity.UnitTests -o backend/tests/Flit.Identity.UnitTests
dotnet new xunit -n Flit.Identity.IntegrationTests -o backend/tests/Flit.Identity.IntegrationTests

cd backend
dotnet sln add src/**/*.csproj tests/**/*.csproj
dotnet add src/Flit.Identity.Api reference src/Flit.Identity.Auth src/Flit.Identity.Users src/Flit.Identity.Rbac src/Flit.Identity.Notifications src/Flit.Identity.Infrastructure
dotnet add src/Flit.Identity.Infrastructure reference src/Flit.Identity.Shared
dotnet add src/Flit.Identity.Auth reference src/Flit.Identity.Infrastructure src/Flit.Identity.Shared
dotnet add src/Flit.Identity.Users reference src/Flit.Identity.Infrastructure src/Flit.Identity.Auth src/Flit.Identity.Rbac src/Flit.Identity.Notifications src/Flit.Identity.Shared
dotnet add src/Flit.Identity.Rbac reference src/Flit.Identity.Infrastructure src/Flit.Identity.Shared
dotnet add src/Flit.Identity.Notifications reference src/Flit.Identity.Shared
dotnet add tests/Flit.Identity.UnitTests reference src/Flit.Identity.Auth src/Flit.Identity.Rbac src/Flit.Identity.Users
dotnet add tests/Flit.Identity.IntegrationTests reference src/Flit.Identity.Api

dotnet add src/Flit.Identity.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/Flit.Identity.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/Flit.Identity.Infrastructure package Konscious.Security.Cryptography.Argon2
dotnet add src/Flit.Identity.Auth package System.IdentityModel.Tokens.Jwt
dotnet add src/Flit.Identity.Auth package Microsoft.IdentityModel.Tokens
dotnet add src/Flit.Identity.Notifications package MailKit
dotnet add tests/Flit.Identity.IntegrationTests package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/Flit.Identity.IntegrationTests package Testcontainers.PostgreSql
```

- [ ] **Step 4: Generate RS256 dev keys**

```bash
openssl genrsa -out backend/keys/jwt-private.pem 2048
openssl rsa -in backend/keys/jwt-private.pem -pubout -out backend/keys/jwt-public.pem
```

- [ ] **Step 5: Configure YARP gateway**

```csharp
// gateway/Flit.Gateway/Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
var app = builder.Build();
app.MapReverseProxy();
app.Run();
```

```json
// gateway/Flit.Gateway/appsettings.Development.json
{
  "ReverseProxy": {
    "Routes": {
      "api": {
        "ClusterId": "identity-api",
        "Match": { "Path": "/api/{**catch-all}" }
      },
      "next": {
        "ClusterId": "nextjs",
        "Match": { "Path": "{**catch-all}" }
      }
    },
    "Clusters": {
      "identity-api": {
        "Destinations": {
          "d1": { "Address": "http://localhost:5080" }
        }
      },
      "nextjs": {
        "Destinations": {
          "d1": { "Address": "http://localhost:3000" }
        }
      }
    }
  }
}
```

Run gateway: `dotnet run --project gateway/Flit.Gateway --urls http://localhost:5000`

- [ ] **Step 6: Commit scaffold**

```bash
git add docker backend gateway
git commit -m "chore(identity): scaffold backend solution, gateway, and docker dev stack"
```

---

## Task 1: [#9711] PostgreSQL schema, EF Core migrations, and seed

**Files:**
- Create: `src/Flit.Identity.Shared/Domain/*.cs` (enums)
- Create: `src/Flit.Identity.Infrastructure/Persistence/Entities/*.cs`
- Create: `src/Flit.Identity.Infrastructure/Persistence/IdentityDbContext.cs`
- Create: `src/Flit.Identity.Infrastructure/Persistence/Seed/IdentityDbSeeder.cs`
- Modify: `src/Flit.Identity.Api/Program.cs`
- Test: `tests/Flit.Identity.IntegrationTests/Database/MigrationSeedTests.cs`

- [ ] **Step 1: Write failing integration test for seed data**

```csharp
// tests/Flit.Identity.IntegrationTests/Database/MigrationSeedTests.cs
using Flit.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Database;

public class MigrationSeedTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public MigrationSeedTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Seed_creates_super_admin_role_and_bootstrap_user()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var superRole = await db.Roles.SingleAsync(r => r.IsSystem);
        var superUser = await db.Users.SingleAsync(u => u.Email == "super@flit.local");

        Assert.Null(superUser.TenantId);
        Assert.Equal(1, superUser.TokenVersion);
        Assert.Contains(superRole.Id, db.UserRoles.Where(ur => ur.UserId == superUser.Id).Select(ur => ur.RoleId));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/tests/Flit.Identity.IntegrationTests --filter MigrationSeedTests -v n`
Expected: FAIL — `IdentityWebApplicationFactory` / DbContext not defined

- [ ] **Step 3: Implement domain enums**

```csharp
// src/Flit.Identity.Shared/Domain/UserStatus.cs
namespace Flit.Identity.Shared.Domain;

public enum UserStatus { Pending, Active, Blocked }
```

```csharp
// src/Flit.Identity.Shared/Domain/PermissionScope.cs
namespace Flit.Identity.Shared.Domain;

public enum PermissionScope { Global, Tenant, Own }
```

```csharp
// src/Flit.Identity.Shared/Domain/PermissionType.cs
namespace Flit.Identity.Shared.Domain;

public enum PermissionType { Crud, Ui }
```

```csharp
// src/Flit.Identity.Shared/Domain/ResetTokenType.cs
namespace Flit.Identity.Shared.Domain;

public enum ResetTokenType { SelfService, Forced }
```

- [ ] **Step 4: Implement core entities**

```csharp
// src/Flit.Identity.Infrastructure/Persistence/Entities/Tenant.cs
namespace Flit.Identity.Infrastructure.Persistence.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];
}
```

```csharp
// src/Flit.Identity.Infrastructure/Persistence/Entities/User.cs
using Flit.Identity.Shared.Domain;

namespace Flit.Identity.Infrastructure.Persistence.Entities;

public class User
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Email { get; set; } = "";
    public string? PasswordHash { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Pending;
    public int TokenVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
```

```csharp
// src/Flit.Identity.Infrastructure/Persistence/Entities/Role.cs
namespace Flit.Identity.Infrastructure.Persistence.Entities;

public class Role
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Name { get; set; } = "";
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
```

```csharp
// src/Flit.Identity.Infrastructure/Persistence/Entities/Permission.cs
using Flit.Identity.Shared.Domain;

namespace Flit.Identity.Infrastructure.Persistence.Entities;

public class Permission
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";
    public PermissionType Type { get; set; }
    public string? Module { get; set; }
    public string Description { get; set; } = "";
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
```

```csharp
// src/Flit.Identity.Infrastructure/Persistence/Entities/RolePermission.cs
using Flit.Identity.Shared.Domain;

namespace Flit.Identity.Infrastructure.Persistence.Entities;

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public PermissionScope Scope { get; set; }
}
```

```csharp
// src/Flit.Identity.Infrastructure/Persistence/Entities/UserRole.cs
namespace Flit.Identity.Infrastructure.Persistence.Entities;

public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
```

```csharp
// src/Flit.Identity.Infrastructure/Persistence/Entities/RefreshToken.cs
namespace Flit.Identity.Infrastructure.Persistence.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

```csharp
// src/Flit.Identity.Infrastructure/Persistence/Entities/InvitationToken.cs
namespace Flit.Identity.Infrastructure.Persistence.Entities;

public class InvitationToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public Guid InvitedBy { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}
```

```csharp
// src/Flit.Identity.Infrastructure/Persistence/Entities/PasswordResetToken.cs
using Flit.Identity.Shared.Domain;

namespace Flit.Identity.Infrastructure.Persistence.Entities;

public class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public ResetTokenType Type { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}
```

- [ ] **Step 5: Implement DbContext with indexes and composite keys**

```csharp
// src/Flit.Identity.Infrastructure/Persistence/IdentityDbContext.cs
using Flit.Identity.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Infrastructure.Persistence;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<InvitationToken> InvitationTokens => Set<InvitationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Tenant).WithMany(t => t.Users).HasForeignKey(x => x.TenantId);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            e.HasOne(x => x.Tenant).WithMany(t => t.Roles).HasForeignKey(x => x.TenantId);
        });

        modelBuilder.Entity<Permission>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(e =>
        {
            e.HasKey(x => new { x.RoleId, x.PermissionId, x.Scope });
            e.HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId);
            e.HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId);
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId);
        });
    }
}
```

- [ ] **Step 6: Implement seeder**

```csharp
// src/Flit.Identity.Infrastructure/Persistence/Seed/IdentityDbSeeder.cs
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Flit.Identity.Infrastructure.Persistence.Seed;

public static class IdentityDbSeeder
{
    public static async Task SeedAsync(IdentityDbContext db, IConfiguration config, IPasswordHasher hasher, CancellationToken ct = default)
    {
        if (await db.Permissions.AnyAsync(ct)) return;

        var modules = new[] { "users", "roles", "tramites" };
        var permissions = new List<Permission>();

        foreach (var module in modules)
        {
            foreach (var action in new[] { "create", "read", "update", "delete" })
            {
                permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Key = $"{module}:{action}",
                    Type = PermissionType.Crud,
                    Module = module,
                    Description = $"{module} {action}"
                });
            }
        }

        permissions.Add(new Permission
        {
            Id = Guid.NewGuid(),
            Key = "generar_consolidado",
            Type = PermissionType.Ui,
            Module = null,
            Description = "Generar consolidado"
        });

        db.Permissions.AddRange(permissions);

        var superRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = null,
            Name = "SuperAdmin",
            IsSystem = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Roles.Add(superRole);

        foreach (var permission in permissions)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = superRole.Id,
                PermissionId = permission.Id,
                Scope = PermissionScope.Global
            });
        }

        var email = config["Identity:BootstrapEmail"] ?? "super@flit.local";
        var password = config["Identity:BootstrapPassword"] ?? "ChangeMe!123";
        var superUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = hasher.Hash(password),
            Status = UserStatus.Active,
            TokenVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ActivatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(superUser);
        db.UserRoles.Add(new UserRole { UserId = superUser.Id, RoleId = superRole.Id });

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 7: Wire Program.cs, create migration, run seeder on startup**

```csharp
// src/Flit.Identity.Api/Program.cs (excerpt)
builder.Services.AddDbContext<IdentityDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Identity")));
builder.Services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await db.Database.MigrateAsync();
    await IdentityDbSeeder.SeedAsync(db, app.Configuration,
        scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.Run();
```

```bash
dotnet ef migrations add InitialIdentitySchema \
  --project backend/src/Flit.Identity.Infrastructure \
  --startup-project backend/src/Flit.Identity.Api
```

- [ ] **Step 8: Run test to verify it passes**

Run: `dotnet test backend/tests/Flit.Identity.IntegrationTests --filter MigrationSeedTests -v n`
Expected: PASS

- [ ] **Step 9: Commit**

```bash
git add backend
git commit -m "feat(identity): add PostgreSQL schema, EF migrations, and seed data (#9711)"
```

---

## Task 2: [#9712] JWT login, refresh, logout, and httpOnly cookies

**Files:**
- Create: `src/Flit.Identity.Infrastructure/Security/Argon2PasswordHasher.cs`
- Create: `src/Flit.Identity.Infrastructure/Security/TokenHasher.cs`
- Create: `src/Flit.Identity.Infrastructure/Security/RsaJwtService.cs`
- Create: `src/Flit.Identity.Infrastructure/Security/CookieAuthOptions.cs`
- Create: `src/Flit.Identity.Auth/PermissionResolver.cs`
- Create: `src/Flit.Identity.Auth/LoginHandler.cs`
- Create: `src/Flit.Identity.Auth/RefreshHandler.cs`
- Create: `src/Flit.Identity.Auth/LogoutHandler.cs`
- Create: `src/Flit.Identity.Auth/MeHandler.cs`
- Create: `src/Flit.Identity.Auth/AuthEndpoints.cs`
- Test: `tests/Flit.Identity.IntegrationTests/Auth/LoginRefreshLogoutTests.cs`

- [ ] **Step 1: Write failing login integration test**

```csharp
// tests/Flit.Identity.IntegrationTests/Auth/LoginRefreshLogoutTests.cs
public class LoginRefreshLogoutTests : IClassFixture<IdentityWebApplicationFactory>
{
  [Fact]
  public async Task Login_sets_httpOnly_cookies_and_me_returns_user()
  {
    var client = _factory.CreateClient();
    var login = await client.PostAsJsonAsync("/api/auth/login",
      new { email = "super@flit.local", password = "ChangeMe!123" });
    login.EnsureSuccessStatusCode();

    Assert.Contains(client.DefaultRequestHeaders.GetValues("Cookie"), c => c.Contains("flit_access="));
    Assert.Contains(client.DefaultRequestHeaders.GetValues("Cookie"), c => c.Contains("flit_refresh="));

    var me = await client.GetAsync("/api/auth/me");
    me.EnsureSuccessStatusCode();
    var body = await me.Content.ReadFromJsonAsync<MeResponse>();
    Assert.Equal("super@flit.local", body!.Email);
  }
}
```

- [ ] **Step 2: Run test — expect FAIL** (`404` or handler missing)

- [ ] **Step 3: Implement password hasher and token hasher**

```csharp
// src/Flit.Identity.Infrastructure/Security/IPasswordHasher.cs
namespace Flit.Identity.Infrastructure.Security;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
```

```csharp
// src/Flit.Identity.Infrastructure/Security/Argon2PasswordHasher.cs
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography.Argon2;

namespace Flit.Identity.Infrastructure.Security;

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 2,
            MemorySize = 65536,
            Iterations = 3
        };
        var hash = argon2.GetBytes(32);
        return $"$argon2id$v=19$m=65536,t=3,p=2${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string encoded)
    {
        var parts = encoded.Split('$', StringSplitOptions.RemoveEmptyEntries);
        var salt = Convert.FromBase64String(parts[3]);
        var expected = Convert.FromBase64String(parts[4]);
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 2,
            MemorySize = 65536,
            Iterations = 3
        };
        return CryptographicOperations.FixedTimeEquals(argon2.GetBytes(32), expected);
    }
}
```

```csharp
// src/Flit.Identity.Infrastructure/Security/TokenHasher.cs
using System.Security.Cryptography;
using System.Text;

namespace Flit.Identity.Infrastructure.Security;

public static class TokenHasher
{
    public static string Sha256(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
}
```

- [ ] **Step 4: Implement PermissionResolver (multi-role union)**

```csharp
// src/Flit.Identity.Auth/PermissionResolver.cs
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Auth;

public sealed class PermissionResolver(IdentityDbContext db)
{
    public async Task<IReadOnlyList<PermissionGrant>> ResolveAsync(User user, CancellationToken ct)
    {
        if (user.TenantId is null)
        {
            return await db.RolePermissions
                .Where(rp => rp.Role.IsSystem)
                .Select(rp => new PermissionGrant(rp.Permission.Key, rp.Scope))
                .Distinct()
                .ToListAsync(ct);
        }

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToArray();
        var grants = await db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => new PermissionGrant(rp.Permission.Key, rp.Scope))
            .ToListAsync(ct);

        return grants
            .GroupBy(g => (g.Key, g.Scope))
            .Select(g => g.First())
            .ToList();
    }
}
```

```csharp
// src/Flit.Identity.Shared/Auth/PermissionGrant.cs
using Flit.Identity.Shared.Domain;

namespace Flit.Identity.Shared.Auth;

public record PermissionGrant(string Key, PermissionScope Scope);
```

- [ ] **Step 5: Implement RsaJwtService**

```csharp
// src/Flit.Identity.Infrastructure/Security/RsaJwtService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Flit.Identity.Shared.Auth;
using Microsoft.IdentityModel.Tokens;

namespace Flit.Identity.Infrastructure.Security;

public sealed class RsaJwtService(IConfiguration config)
{
    private readonly RsaSecurityKey _signingKey = LoadPrivateKey(config["Jwt:PrivateKeyPath"]!);
    private readonly RsaSecurityKey _validationKey = LoadPublicKey(config["Jwt:PublicKeyPath"]!);

    public string CreateAccessToken(Guid userId, string email, Guid? tenantId, IReadOnlyList<string> roles,
        IReadOnlyList<PermissionGrant> permissions, bool isSuperAdmin, int tokenVersion)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("token_version", tokenVersion.ToString()),
            new("is_super_admin", isSuperAdmin ? "true" : "false"),
        };
        if (tenantId is not null) claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));
        foreach (var role in roles) claims.Add(new Claim("roles", role));
        foreach (var p in permissions)
            claims.Add(new Claim("permissions", $"{p.Key}|{p.Scope}"));

        var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal Validate(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = config["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _validationKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        }, out _);
    }

    private static RsaSecurityKey LoadPrivateKey(string path)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(path));
        return new RsaSecurityKey(rsa);
    }

    private static RsaSecurityKey LoadPublicKey(string path)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(path));
        return new RsaSecurityKey(rsa);
    }
}
```

- [ ] **Step 6: Implement LoginHandler with cookie issuance**

```csharp
// src/Flit.Identity.Auth/LoginHandler.cs
public sealed class LoginHandler(IdentityDbContext db, IPasswordHasher hasher,
    PermissionResolver resolver, RsaJwtService jwt)
{
    public async Task<IResult> HandleAsync(LoginRequest req, HttpContext http, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant(), ct);

        if (user is null || user.Status != UserStatus.Active || user.PasswordHash is null
            || !hasher.Verify(req.Password, user.PasswordHash))
        {
            return Results.Json(new { code = "INVALID_CREDENTIALS" }, statusCode: 401);
        }

        var permissions = await resolver.ResolveAsync(user, ct);
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var access = jwt.CreateAccessToken(user.Id, user.Email, user.TenantId, roles, permissions,
            user.TenantId is null, user.TokenVersion);

        var refreshRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = TokenHasher.Sha256(refreshRaw),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);

        http.Response.Cookies.Append("flit_access", access, CookieOptions.Access(15));
        http.Response.Cookies.Append("flit_refresh", refreshRaw, CookieOptions.Refresh(7));

        return Results.Ok(new { id = user.Id, email = user.Email, tenant_id = user.TenantId, roles });
    }
}
```

```csharp
// src/Flit.Identity.Infrastructure/Security/CookieAuthOptions.cs
namespace Flit.Identity.Infrastructure.Security;

public static class CookieOptions
{
    public static Microsoft.AspNetCore.Http.CookieOptions Access(int minutes) => new()
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
        Path = "/", MaxAge = TimeSpan.FromMinutes(minutes)
    };

    public static Microsoft.AspNetCore.Http.CookieOptions Refresh(int days) => new()
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
        Path = "/", MaxAge = TimeSpan.FromDays(days)
    };

    public static Microsoft.AspNetCore.Http.CookieOptions Delete() => new()
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
        Path = "/", Expires = DateTimeOffset.UnixEpoch
    };
}
```

- [ ] **Step 7: Implement refresh, logout, me endpoints and register routes**

```csharp
// src/Flit.Identity.Auth/AuthEndpoints.cs
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");
        group.MapPost("/login", (LoginRequest req, LoginHandler h, HttpContext ctx, CancellationToken ct) =>
            h.HandleAsync(req, ctx, ct));
        group.MapPost("/refresh", RefreshHandler.Handle);
        group.MapPost("/logout", LogoutHandler.Handle);
        group.MapGet("/me", MeHandler.Handle);
        return app;
    }
}
```

Implement `RefreshHandler` to: validate `flit_refresh` cookie hash, check `token_version`, rotate refresh token, recompute permissions, set new cookies.

Implement `LogoutHandler` to: revoke current refresh token, delete both cookies, return `204`.

Implement `MeHandler` to: read `flit_access`, validate JWT, return `{ user, roles, permissions[] }`.

- [ ] **Step 8: Add JWT auth middleware reading `flit_access` cookie**

```csharp
// src/Flit.Identity.Api/Middleware/JwtCookieAuthenticationMiddleware.cs
public sealed class JwtCookieAuthenticationMiddleware(RequestDelegate next, RsaJwtService jwt)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue("flit_access", out var token))
        {
            try
            {
                var principal = jwt.Validate(token);
                context.User = principal;
            }
            catch (SecurityTokenExpiredException)
            {
                // leave unauthenticated; refresh endpoint handles renewal
            }
        }
        await next(context);
    }
}
```

- [ ] **Step 9: Run integration tests**

Run: `dotnet test backend/tests/Flit.Identity.IntegrationTests --filter LoginRefreshLogoutTests -v n`
Expected: PASS (add refresh + logout test cases in same file)

- [ ] **Step 10: Commit**

```bash
git commit -am "feat(identity): JWT login, refresh, logout with httpOnly cookies (#9712)"
```

---

## Task 3: [#9713] RBAC catalog, scopes, and EF tenant filter

**Files:**
- Create: `src/Flit.Identity.Infrastructure/Tenancy/ITenantContext.cs`
- Create: `src/Flit.Identity.Infrastructure/Tenancy/TenantContext.cs`
- Create: `src/Flit.Identity.Rbac/AuthorizationService.cs`
- Create: `src/Flit.Identity.Rbac/Endpoints/RolesEndpoints.cs`
- Create: `src/Flit.Identity.Rbac/Endpoints/PermissionsEndpoints.cs`
- Create: `src/Flit.Identity.Api/Middleware/TenantResolutionMiddleware.cs`
- Test: `tests/Flit.Identity.UnitTests/Rbac/AuthorizationServiceTests.cs`
- Test: `tests/Flit.Identity.IntegrationTests/Rbac/TenantIsolationTests.cs`

- [ ] **Step 1: Write failing unit tests for scope evaluation**

```csharp
// tests/Flit.Identity.UnitTests/Rbac/AuthorizationServiceTests.cs
public class AuthorizationServiceTests
{
    private readonly AuthorizationService _svc = new();

    [Fact]
    public void SuperAdmin_bypasses_all_checks()
    {
        var user = new CurrentUser(Guid.NewGuid(), null, true, 1, []);
        Assert.True(_svc.CanAccess(user, "tramites:read", new ResourceContext(Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public void Own_scope_requires_matching_owner()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var user = new CurrentUser(userId, tenantId, false, 1,
            [new PermissionGrant("tramites:read", PermissionScope.Own)]);

        Assert.True(_svc.CanAccess(user, "tramites:read",
            new ResourceContext(tenantId, userId)));
        Assert.False(_svc.CanAccess(user, "tramites:read",
            new ResourceContext(tenantId, Guid.NewGuid())));
    }

    [Fact]
    public void Multi_role_union_allows_if_any_role_grants()
    {
        var tenantId = Guid.NewGuid();
        var user = new CurrentUser(Guid.NewGuid(), tenantId, false, 1,
        [
            new PermissionGrant("tramites:read", PermissionScope.Own),
            new PermissionGrant("tramites:read", PermissionScope.Tenant)
        ]);
        Assert.True(_svc.CanAccess(user, "tramites:read",
            new ResourceContext(tenantId, Guid.NewGuid())));
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

- [ ] **Step 3: Implement AuthorizationService per design §4**

```csharp
// src/Flit.Identity.Rbac/AuthorizationService.cs
public sealed class AuthorizationService
{
    public bool CanAccess(CurrentUser user, string permissionKey, ResourceContext? resource = null)
    {
        if (user.IsSuperAdmin) return true;

        var grants = user.Permissions.Where(p => p.Key == permissionKey).ToList();
        if (grants.Count == 0) return false;

        foreach (var grant in grants)
        {
            switch (grant.Scope)
            {
                case PermissionScope.Global:
                    return true;
                case PermissionScope.Tenant:
                    if (resource is not null && resource.TenantId == user.TenantId) return true;
                    break;
                case PermissionScope.Own:
                    if (resource is not null && resource.TenantId == user.TenantId
                        && resource.OwnerId == user.Id) return true;
                    break;
            }
        }
        return false;
    }
}
```

- [ ] **Step 4: Add global EF tenant query filter on `Role`**

```csharp
// src/Flit.Identity.Infrastructure/Persistence/IdentityDbContext.cs (add)
private readonly ITenantContext _tenant = tenantContext;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ...existing config...
    modelBuilder.Entity<Role>().HasQueryFilter(r =>
        _tenant.CurrentTenantId == null || r.TenantId == _tenant.CurrentTenantId);
}
```

- [ ] **Step 5: Implement tenant resolution middleware from JWT claims**

```csharp
// src/Flit.Identity.Api/Middleware/TenantResolutionMiddleware.cs
public sealed class TenantResolutionMiddleware(RequestDelegate next, ITenantContext tenant)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is not null)
        {
            tenant.UserId = Guid.Parse(sub);
            tenant.IsSuperAdmin = context.User.FindFirst("is_super_admin")?.Value == "true";
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
            tenant.CurrentTenantId = tenantClaim is null ? null : Guid.Parse(tenantClaim);
            tenant.TokenVersion = int.Parse(context.User.FindFirst("token_version")!.Value);
        }
        await next(context);
    }
}
```

- [ ] **Step 6: Implement roles + permissions endpoints**

`GET /api/permissions` — returns catalog (tenant admins read-only).

`GET/POST/PUT/DELETE /api/roles` — tenant-scoped CRUD; block update/delete when `IsSystem`.

`PUT /api/roles/{id}/permissions` — body `{ permissions: [{ permission_id, scope }] }`.

Add `RequirePermission("roles:read", PermissionScope.Tenant)` authorization filter attribute.

- [ ] **Step 7: Write integration test — tenant A cannot list tenant B roles**

```csharp
[Fact]
public async Task Tenant_admin_cannot_see_other_tenant_roles()
{
    // seed tenant A admin + tenant B role via factory helper
    // authenticate as tenant A admin
    // GET /api/roles should not include tenant B role names
}
```

- [ ] **Step 8: Run tests — expect PASS**

Run: `dotnet test backend/tests -v n`

- [ ] **Step 9: Commit**

```bash
git commit -am "feat(identity): RBAC catalog, scope evaluation, tenant EF filter (#9713)"
```

---

## Task 4: [#9714] Email invitation, activation, and tenant inheritance

**Files:**
- Create: `src/Flit.Identity.Users/InviteUserHandler.cs`
- Create: `src/Flit.Identity.Auth/ActivateHandler.cs`
- Create: `src/Flit.Identity.Notifications/EmailSender.cs`
- Create: `src/Flit.Identity.Notifications/Templates/InvitationEmail.cs`
- Modify: `src/Flit.Identity.Users/Endpoints/UsersEndpoints.cs`
- Test: `tests/Flit.Identity.IntegrationTests/Users/InvitationActivationTests.cs`

- [ ] **Step 1: Write failing test — invite creates Pending user and activation works**

```csharp
[Fact]
public async Task Invite_then_activate_allows_login()
{
    var admin = await _factory.LoginAsTenantAdminAsync();
    var invite = await admin.PostAsJsonAsync("/api/users/invite",
        new { email = "new.user@tenant-a.com", role_ids = new[] { _factory.TenantOperatorRoleId } });
    invite.EnsureSuccessStatusCode();

    var token = await _factory.GetLatestInvitationTokenAsync("new.user@tenant-a.com");
    var activate = await _factory.AnonymousClient.PostAsJsonAsync("/api/auth/activate",
        new { token, password = "SecurePass!123" });
    activate.EnsureSuccessStatusCode();

    var login = await _factory.AnonymousClient.PostAsJsonAsync("/api/auth/login",
        new { email = "new.user@tenant-a.com", password = "SecurePass!123" });
    login.EnsureSuccessStatusCode();
}
```

- [ ] **Step 2: Run test — expect FAIL**

- [ ] **Step 3: Implement InviteUserHandler**

```csharp
public sealed class InviteUserHandler(IdentityDbContext db, EmailSender email, IConfiguration config)
{
    public async Task<IResult> HandleAsync(InviteRequest req, CurrentUser inviter, CancellationToken ct)
    {
        var tenantId = inviter.IsSuperAdmin ? req.TenantId : inviter.TenantId;
        if (tenantId is null) return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["tenant_id"] = ["Required for SuperAdmin invites."]
        });

        if (await db.Users.AnyAsync(u => u.Email == req.Email.ToLowerInvariant(), ct))
            return Results.Conflict(new { code = "EMAIL_EXISTS" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = req.Email.ToLowerInvariant(),
            Status = UserStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);
        foreach (var roleId in req.RoleIds)
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });

        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        db.InvitationTokens.Add(new InvitationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = TokenHasher.Sha256(raw),
            InvitedBy = inviter.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(72)
        });
        await db.SaveChangesAsync(ct);

        var link = $"{config["App:PublicBaseUrl"]}/activate?token={Uri.EscapeDataString(raw)}";
        await email.SendInvitationAsync(user.Email, link, ct);

        return Results.Created($"/api/users/{user.Id}",
            new { user_id = user.Id, email = user.Email, status = "Pending" });
    }
}
```

- [ ] **Step 4: Implement ActivateHandler**

Validate token hash, reject expired/used (CF-C6), set `PasswordHash`, `Status=Active`, `ActivatedAt`, mark token used.

- [ ] **Step 5: Implement MailKit EmailSender targeting Mailhog**

```csharp
// appsettings.Development.json
"Smtp": { "Host": "localhost", "Port": 1025, "UseSsl": false, "From": "identity@flit.local" }
```

- [ ] **Step 6: Add test for SuperAdmin required `tenant_id` and tenant admin inheritance**

- [ ] **Step 7: Run tests — expect PASS**

- [ ] **Step 8: Commit**

```bash
git commit -am "feat(identity): invitation, activation, tenant inheritance (#9714)"
```

---

## Task 5: [#9715] Forgot password, forced reset, and SMTP

**Files:**
- Create: `src/Flit.Identity.Auth/ForgotPasswordHandler.cs`
- Create: `src/Flit.Identity.Auth/ResetPasswordHandler.cs`
- Modify: `src/Flit.Identity.Users/Endpoints/UsersEndpoints.cs` (`force-reset`)
- Test: `tests/Flit.Identity.IntegrationTests/Auth/PasswordResetTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public async Task Forgot_password_always_returns_200_even_for_unknown_email()
{
    var res = await _client.PostAsJsonAsync("/api/auth/forgot-password",
        new { email = "nobody@example.com" });
    Assert.Equal(HttpStatusCode.OK, res.StatusCode);
}

[Fact]
public async Task Reset_password_bumps_token_version_and_revokes_sessions()
{
    var user = await _factory.CreateActiveUserAsync();
    var cookies = await _factory.LoginAndGetCookiesAsync(user.Email);

    var raw = await _factory.CreateSelfServiceResetTokenAsync(user.Email);
    var reset = await _client.PostAsJsonAsync("/api/auth/reset-password",
        new { token = raw, new_password = "NewPass!456" });
    reset.EnsureSuccessStatusCode();

    var me = await _factory.SendWithCookiesAsync("/api/auth/me", cookies);
    Assert.Equal(HttpStatusCode.Forbidden, me.StatusCode);
    Assert.Equal("SESSION_REVOKED", await me.ReadErrorCodeAsync());
}
```

- [ ] **Step 2: Implement forgot-password (always 200, rate limit 5/15min per IP+email)**

- [ ] **Step 3: Implement reset-password (SelfService: 1h expiry, revoke refresh tokens, bump token_version)**

- [ ] **Step 4: Implement `POST /api/users/{id}/force-reset` (Forced: 24h, email, bump token_version)**

- [ ] **Step 5: Run tests — expect PASS**

- [ ] **Step 6: Commit**

```bash
git commit -am "feat(identity): forgot/forced password reset flows (#9715)"
```

---

## Task 6: [#9716] Session eviction via token_version

**Files:**
- Create: `src/Flit.Identity.Auth/SessionRevocationService.cs`
- Modify: all privilege-mutating handlers to call revocation service
- Create: `src/Flit.Identity.Api/Middleware/TokenVersionValidationMiddleware.cs`
- Test: `tests/Flit.Identity.IntegrationTests/Auth/SessionEvictionTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public async Task Role_change_revokes_existing_access_token()
{
    var admin = await _factory.LoginAsTenantAdminAsync();
    var target = await _factory.CreateActiveUserAsync();

    var cookies = await _factory.LoginAndGetCookiesAsync(target.Email);

    await admin.PutAsJsonAsync($"/api/users/{target.Id}/roles",
        new { role_ids = new[] { _factory.TenantAdminRoleId }, confirm = true });

    var res = await _factory.SendWithCookiesAsync("/api/auth/me", cookies);
    Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    Assert.True(res.Headers.Contains("X-Session-Revoked"));
}
```

- [ ] **Step 2: Implement SessionRevocationService**

```csharp
public sealed class SessionRevocationService(IdentityDbContext db)
{
    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.SingleAsync(u => u.Id == userId, ct);
        user.TokenVersion += 1;
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in tokens) t.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 3: Add TokenVersionValidationMiddleware**

After JWT validation, compare claim `token_version` with DB value (cache per request). On mismatch return:

```csharp
return Results.Json(new { code = "SESSION_REVOKED", message = "Session revoked." },
    statusCode: 403, options: null, contentType: null);
// plus header X-Session-Revoked: true
```

Wire revocation into: `PUT /api/users/{id}/roles`, `PUT /api/roles/{id}/permissions`, role migrate/delete, force-reset, user block.

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(identity): session eviction via token_version (#9716)"
```

---

## Task 7: [#9717] Role conflicts and batch migration

**Files:**
- Create: `src/Flit.Identity.Rbac/RoleConflictAnalyzer.cs`
- Modify: `src/Flit.Identity.Users/Endpoints/UsersEndpoints.cs`
- Modify: `src/Flit.Identity.Rbac/Endpoints/RolesEndpoints.cs`
- Test: `tests/Flit.Identity.UnitTests/Rbac/RoleConflictAnalyzerTests.cs`
- Test: `tests/Flit.Identity.IntegrationTests/Rbac/RoleMigrationTests.cs`

- [ ] **Step 1: Write failing unit tests for conflict detection**

```csharp
[Fact]
public void Detects_redundant_role_when_permissions_subset()
{
    var analyzer = new RoleConflictAnalyzer();
    var warnings = analyzer.Analyze(
        current: [new PermissionGrant("tramites:read", PermissionScope.Own)],
        proposed: [
            new PermissionGrant("tramites:read", PermissionScope.Own),
            new PermissionGrant("tramites:read", PermissionScope.Tenant)
        ]);
    Assert.Contains(warnings, w => w.Type == "redundancy");
}
```

- [ ] **Step 2: Implement RoleConflictAnalyzer (redundancy + incompatible scopes)**

- [ ] **Step 3: Update `PUT /api/users/{id}/roles`**

If warnings exist and `confirm != true` → `200 { warnings, pending: true }`.
If `confirm == true` → apply, call `SessionRevocationService`.

- [ ] **Step 4: Implement role delete guard and migrate**

`DELETE /api/roles/{id}` → `409 ROLE_HAS_USERS` when `user_roles` count > 0.

`POST /api/roles/{id}/migrate { replacement_role_id }` → reassign all users, delete role, revoke sessions for affected users.

- [ ] **Step 5: Run tests — expect PASS**

- [ ] **Step 6: Commit**

```bash
git commit -am "feat(identity): role conflict warnings and batch migration (#9717)"
```

---

## Task 8: [#9718] Next.js middleware, login, and credential flows

**Files (in `/home/david/davidch/frontend/`):**
- Create: `middleware.ts`
- Create: `lib/auth/cookies.ts`
- Create: `lib/auth/api-client.ts`
- Create: `lib/auth/session.ts`
- Create: `app/login/page.tsx`
- Create: `app/activate/page.tsx`
- Create: `app/forgot-password/page.tsx`
- Create: `app/reset-password/page.tsx`
- Create: `components/auth/SessionRevokedBanner.tsx`
- Modify: `next.config.ts` (if needed for gateway proxy in dev)

- [ ] **Step 1: Add env for API base URL**

```bash
# frontend/.env.local
API_BASE_URL=http://localhost:5000
```

- [ ] **Step 2: Implement server-side API client forwarding cookies**

```typescript
// lib/auth/api-client.ts
import { cookies } from "next/headers";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

export async function apiFetch(path: string, init: RequestInit = {}) {
  const jar = await cookies();
  const cookieHeader = jar
    .getAll()
    .filter((c) => c.name === "flit_access" || c.name === "flit_refresh")
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(cookieHeader ? { Cookie: cookieHeader } : {}),
      ...init.headers,
    },
    cache: "no-store",
  });

  if (res.status === 403) {
    const body = await res.clone().json().catch(() => ({}));
    if (body.code === "SESSION_REVOKED") {
      throw new SessionRevokedError();
    }
  }
  return res;
}

export class SessionRevokedError extends Error {
  constructor() {
    super("SESSION_REVOKED");
  }
}
```

- [ ] **Step 3: Implement middleware with silent refresh**

```typescript
// middleware.ts
import { NextRequest, NextResponse } from "next/server";

const PUBLIC = ["/login", "/activate", "/forgot-password", "/reset-password"];

export async function middleware(req: NextRequest) {
  const { pathname } = req.nextUrl;
  if (PUBLIC.some((p) => pathname.startsWith(p))) return NextResponse.next();

  const access = req.cookies.get("flit_access");
  if (access) return NextResponse.next();

  const refresh = req.cookies.get("flit_refresh");
  if (!refresh) return NextResponse.redirect(new URL("/login", req.url));

  const refreshRes = await fetch(`${process.env.API_BASE_URL}/api/auth/refresh`, {
    method: "POST",
    headers: { Cookie: `flit_refresh=${refresh.value}` },
  });

  if (!refreshRes.ok) return NextResponse.redirect(new URL("/login", req.url));

  const response = NextResponse.next();
  for (const cookie of refreshRes.headers.getSetCookie?.() ?? []) {
    response.headers.append("Set-Cookie", cookie);
  }
  return response;
}

export const config = { matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"] };
```

- [ ] **Step 4: Build login page (server action posts to `/api/auth/login`)**

```typescript
// app/login/page.tsx
"use client";
import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";

export default function LoginPage() {
  const router = useRouter();
  const params = useSearchParams();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    const res = await fetch("/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
      credentials: "include",
    });
    if (!res.ok) {
      setError("Credenciales inválidas");
      return;
    }
    router.push(params.get("reason") === "session_revoked" ? "/?revoked=1" : "/");
  }

  return (
    <main className="mx-auto max-w-md p-8">
      {params.get("reason") === "session_revoked" && (
        <p role="alert" className="mb-4 rounded bg-amber-100 p-3 text-amber-900">
          Tu sesión fue cerrada por un cambio de permisos. Inicia sesión de nuevo.
        </p>
      )}
      <form onSubmit={onSubmit} className="space-y-4">
        <h1 className="text-2xl font-semibold">Iniciar sesión</h1>
        <input className="w-full border p-2" type="email" value={email}
          onChange={(e) => setEmail(e.target.value)} required />
        <input className="w-full border p-2" type="password" value={password}
          onChange={(e) => setPassword(e.target.value)} required />
        {error && <p className="text-red-600">{error}</p>}
        <button className="w-full bg-black text-white p-2" type="submit">Entrar</button>
      </form>
    </main>
  );
}
```

- [ ] **Step 5: Build activate, forgot-password, reset-password pages** (forms call `/api/auth/activate`, `/api/auth/forgot-password`, `/api/auth/reset-password`)

- [ ] **Step 6: Verify manually**

Run gateway on `:5000`, API on `:5080`, `npm run dev` in flit2.
Visit http://localhost:5000/login → login as `super@flit.local` → redirect to home with session.

- [ ] **Step 7: Commit in flit2 repo**

```bash
cd /home/david/davidch/frontend
git add middleware.ts lib/auth app/login app/activate app/forgot-password app/reset-password components/auth
git commit -m "feat(identity): auth middleware and credential flows (#9718)"
```

---

## Task 9: [#9719] Admin console — users, roles, PermissionGuard

**Files (in `/home/david/davidch/frontend/`):**
- Create: `lib/auth/permissions.ts`
- Create: `components/auth/Can.tsx`
- Create: `app/admin/users/page.tsx`
- Create: `app/admin/roles/page.tsx`
- Create: `app/admin/roles/[id]/page.tsx`
- Create: `app/admin/layout.tsx`

- [ ] **Step 1: Implement server session helper**

```typescript
// lib/auth/session.ts
import { apiFetch } from "./api-client";

export type MeResponse = {
  id: string;
  email: string;
  tenant_id: string | null;
  roles: string[];
  permissions: { key: string; scope: "Global" | "Tenant" | "Own" }[];
  is_super_admin: boolean;
};

export async function getSession(): Promise<MeResponse | null> {
  const res = await apiFetch("/api/auth/me");
  if (!res.ok) return null;
  return res.json();
}

export function hasPermission(
  session: MeResponse,
  key: string,
  scope: "Global" | "Tenant" | "Own" = "Tenant"
) {
  if (session.is_super_admin) return true;
  return session.permissions.some((p) => p.key === key && p.scope === scope);
}
```

- [ ] **Step 2: Implement `<Can>` component**

```tsx
// components/auth/Can.tsx
import { ReactNode } from "react";
import { hasPermission, MeResponse } from "@/lib/auth/permissions";

export function Can({
  permission,
  scope = "Tenant",
  session,
  children,
}: {
  permission: string;
  scope?: "Global" | "Tenant" | "Own";
  session: MeResponse;
  children: ReactNode;
}) {
  if (!hasPermission(session, permission, scope)) return null;
  return <>{children}</>;
}
```

- [ ] **Step 3: Build `/admin/users` page**

Server component: `getSession()`, redirect if missing `users:read`.
List users from `GET /api/users`, invite form posts to `POST /api/users/invite`, role assignment uses `PUT /api/users/{id}/roles` with conflict warning UI (`confirm` checkbox).

- [ ] **Step 4: Build `/admin/roles` and `/admin/roles/[id]`**

List/create roles; detail page edits permissions via `PUT /api/roles/{id}/permissions`.
Delete button shows 409 message with link to migrate flow (`POST /api/roles/{id}/migrate`).

- [ ] **Step 5: Gate sample action with `generar_consolidado` permission**

Add a demo button on home page wrapped in `<Can permission="generar_consolidado">` to satisfy CF-F1/F2.

- [ ] **Step 6: Verify build**

Run: `cd /home/david/davidch/frontend && npm run build && npm run lint`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git commit -m "feat(identity): admin users/roles console and PermissionGuard (#9719)"
```

---

## Task 10: [#9720] E2E security and multi-tenant identity tests

**Files:**
- Create: `e2e/identity/auth-lifecycle.spec.ts`
- Create: `e2e/identity/session-eviction.spec.ts`
- Create: `e2e/identity/role-migration.spec.ts`
- Create: `e2e/identity/tenant-isolation.spec.ts`
- Create: `playwright.config.ts` (if missing)

- [ ] **Step 1: Add Playwright to flit2**

```bash
cd /home/david/davidch/frontend
npm init playwright@latest
```

- [ ] **Step 2: Write E2E — invite → activate → login → permitted action (CF-I3)**

```typescript
// e2e/identity/auth-lifecycle.spec.ts
import { test, expect } from "@playwright/test";

test("invite activate login and see permitted action", async ({ page }) => {
  await page.goto("/login");
  await page.fill('input[type="email"]', "admin@tenant-a.test");
  await page.fill('input[type="password"]', "AdminPass!123");
  await page.click('button[type="submit"]');

  await page.goto("/admin/users");
  await page.click('text=Invitar usuario');
  await page.fill('input[name="email"]', "operator@tenant-a.test");
  await page.click('button:has-text("Enviar invitación")');

  const token = await test.step("get invitation token from Mailhog", async () => {
    // fetch http://localhost:8025/api/v2/messages and parse activation link
  });

  await page.goto(`/activate?token=${token}`);
  await page.fill('input[type="password"]', "Operator!123");
  await page.click('button[type="submit"]');

  await page.goto("/login");
  await page.fill('input[type="email"]', "operator@tenant-a.test");
  await page.fill('input[type="password"]', "Operator!123");
  await page.click('button[type="submit"]');

  await expect(page.getByRole("button", { name: "Generar consolidado" })).toBeVisible();
});
```

- [ ] **Step 3: Write E2E — permission change → 403 → re-login (CF-I4)**

- [ ] **Step 4: Write E2E — delete role with users → 409 → migrate (CF-I5)**

- [ ] **Step 5: Write E2E — tenant isolation (CF-I1)**

Tenant A admin navigates to `/admin/users`; user emails from tenant B must not appear.

- [ ] **Step 6: Add CI script**

```json
// package.json scripts
"test:e2e:identity": "playwright test e2e/identity"
```

- [ ] **Step 7: Run E2E suite**

Run: `npm run test:e2e:identity`
Expected: all tests PASS against docker stack + gateway + API + Next.js

- [ ] **Step 8: Commit**

```bash
git commit -m "test(identity): E2E security and multi-tenant suite (#9720)"
```

---

## Task 11: Audit logging and rate limiting (cross-cutting, required by spec §7)

**Files:**
- Create: `src/Flit.Identity.Infrastructure/Audit/AuditLog.cs`
- Create: `src/Flit.Identity.Infrastructure/Audit/AuditService.cs`
- Create: `src/Flit.Identity.Api/Middleware/RateLimitingMiddleware.cs`

- [ ] **Step 1: Add `audit_logs` table via migration**

Columns: `id`, `actor_user_id`, `action`, `target_type`, `target_id`, `metadata jsonb`, `created_at`.

- [ ] **Step 2: Log events: login failure, SuperAdmin bypass, privilege changes**

- [ ] **Step 3: Rate limit `/api/auth/login` and `/api/auth/forgot-password` (5 req / 15 min per IP + email)**

- [ ] **Step 4: Integration test for rate limit returns 429 `RATE_LIMITED`**

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(identity): audit logging and auth rate limiting"
```

---

## Spec Coverage Checklist

| Spec section | Task |
|--------------|------|
| §1 Architecture / YARP | Task 0 |
| §2 Data model + seed | Task 1 |
| §3.1 Login | Task 2 |
| §3.2 Session / me | Task 2, 8 |
| §3.3 Refresh | Task 2, 8 |
| §3.4 Invitation | Task 4, 8 |
| §3.5 Password mgmt | Task 5, 8 |
| §3.6 Session eviction | Task 6, 8 |
| §3.7 Role admin | Task 7, 9 |
| §4 Authorization model | Task 3, 9 |
| §5 API surface | Tasks 2–7 |
| §6 Frontend integration | Tasks 8–9 |
| §7 Security (Argon2, RS256, rate limit, audit) | Tasks 2, 11 |
| §8 Testing strategy | Tasks 1–7 unit/integration, Task 10 E2E |
| RF01–RF14 | All tasks above |
| CF-I1–I5 | Tasks 3, 6, 7, 10 |

---

## Local dev quickstart (after all tasks)

```bash
# Terminal 1 — infrastructure
docker compose -f /home/david/davidch/docker/docker-compose.yml up -d

# Terminal 2 — API
cd /home/david/davidch/backend/src/Flit.Identity.Api
dotnet run --urls http://localhost:5080

# Terminal 3 — Next.js
cd /home/david/davidch/frontend && npm run dev

# Terminal 4 — Gateway (unified entry)
cd /home/david/davidch/gateway/Flit.Gateway
dotnet run --urls http://localhost:5000

# Open http://localhost:5000/login
# Mailhog: http://localhost:8025
```

---

## HU completion checklist (per flit-gestion-hu)

For each ADO story #9711–#9720:

1. Move story to **Active** when starting the task block
2. Implement with tests green (`dotnet test`, `npm run build`, E2E for #9720)
3. Move story to **Resolved** with PR link
4. Mention QA on #9720 when E2E suite is green
