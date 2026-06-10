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
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var superRole = await db.Roles.SingleAsync(r => r.IsSystem);
        var superUser = await db.Users.SingleAsync(u => u.Email == "super@flit.local");

        Assert.Null(superUser.TenantId);
        Assert.Equal(1, superUser.TokenVersion);
        Assert.Contains(superRole.Id, db.UserRoles.Where(ur => ur.UserId == superUser.Id).Select(ur => ur.RoleId));
    }
}
