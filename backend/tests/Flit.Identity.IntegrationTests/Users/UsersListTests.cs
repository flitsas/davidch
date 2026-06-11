using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Flit.Identity.IntegrationTests.Users;

public class UsersListTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public UsersListTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task List_users_includes_role_ids()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var admin = await _factory.LoginAsTenantAdminAsync();

        var users = await admin.GetFromJsonAsync<List<UserWithRoles>>("/api/users");
        Assert.NotNull(users);

        var tenantAdmin = users!.Single(u => u.Email == _factory.TenantAdminEmail);
        Assert.Contains(_factory.TenantAdminRoleId, tenantAdmin.RoleIds);
    }

    private record UserWithRoles(
        Guid Id,
        string Email,
        string Status,
        [property: JsonPropertyName("role_ids")] Guid[] RoleIds);
}
