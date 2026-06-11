using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Rbac;

public class RoleMigrationTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public RoleMigrationTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Delete_role_with_assigned_users_returns_409()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var (sourceRoleId, _) = await SeedRolesWithAssignedUserAsync();
        var client = await _factory.LoginAsSuperAdminAsync();

        var response = await client.DeleteAsync($"/api/roles/{sourceRoleId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("ROLE_HAS_USERS", await response.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task Migrate_role_reassigns_users_and_deletes_source()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var (sourceRoleId, replacementRoleId) = await SeedRolesWithAssignedUserAsync();
        var client = await _factory.LoginAsSuperAdminAsync();

        var migrate = await client.PostAsJsonAsync($"/api/roles/{sourceRoleId}/migrate",
            new { replacement_role_id = replacementRoleId });
        migrate.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        Assert.False(await db.Roles.AnyAsync(r => r.Id == sourceRoleId));
        Assert.True(await db.UserRoles.AnyAsync(ur => ur.RoleId == replacementRoleId));
        Assert.False(await db.UserRoles.AnyAsync(ur => ur.RoleId == sourceRoleId));
    }

    private async Task<(Guid SourceRoleId, Guid ReplacementRoleId)> SeedRolesWithAssignedUserAsync()
    {
        await _factory.EnsureTenantSeededAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var sourceRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = _factory.TenantId,
            Name = $"Source-{Guid.NewGuid():N}",
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var replacementRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = _factory.TenantId,
            Name = $"Replacement-{Guid.NewGuid():N}",
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Roles.AddRange(sourceRole, replacementRole);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"role-migrate.{Guid.NewGuid():N}@tenant-a.com",
            PasswordHash = hasher.Hash("SecurePass!123"),
            TenantId = _factory.TenantId,
            Status = UserStatus.Active,
            TokenVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ActivatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = sourceRole.Id });

        await db.SaveChangesAsync();
        return (sourceRole.Id, replacementRole.Id);
    }

    private record ErrorResponse([property: JsonPropertyName("code")] string Code);
}

internal static class RoleMigrationHttpExtensions
{
    public static async Task<string?> ReadErrorCodeAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        return body?.Code;
    }

    private record ErrorBody([property: JsonPropertyName("code")] string Code);
}
