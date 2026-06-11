using System.Net;
using System.Net.Http.Json;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Users;

public class InvitationActivationTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public InvitationActivationTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Invite_then_activate_allows_login()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();

        var admin = await _factory.LoginAsTenantAdminAsync();
        var invite = await admin.PostAsJsonAsync("/api/users/invite",
            new { email = "new.user@tenant-a.com", role_ids = new[] { _factory.TenantOperatorRoleId } });
        invite.EnsureSuccessStatusCode();

        var token = _factory.GetLatestInvitationToken("new.user@tenant-a.com");
        Assert.NotNull(token);

        var activate = await _factory.AnonymousClient.PostAsJsonAsync("/api/auth/activate",
            new { token, password = "SecurePass!123" });
        activate.EnsureSuccessStatusCode();

        var login = await _factory.AnonymousClient.PostAsJsonAsync("/api/auth/login",
            new { email = "new.user@tenant-a.com", password = "SecurePass!123" });
        login.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Invite_creates_pending_user_listed_for_tenant()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();

        var admin = await _factory.LoginAsTenantAdminAsync();
        var invite = await admin.PostAsJsonAsync("/api/users/invite",
            new { email = "pending.user@tenant-a.com", role_ids = Array.Empty<Guid>() });
        invite.EnsureSuccessStatusCode();

        var users = await admin.GetFromJsonAsync<List<UserSummary>>("/api/users");
        Assert.NotNull(users);
        Assert.Contains(users, u => u.Email == "pending.user@tenant-a.com" && u.Status == "Pending");
    }

    [Fact]
    public async Task SuperAdmin_invite_requires_tenant_id()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();

        var superAdmin = await _factory.LoginAsSuperAdminAsync();
        var withoutTenant = await superAdmin.PostAsJsonAsync("/api/users/invite",
            new { email = "orphan@flit.local", role_ids = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.BadRequest, withoutTenant.StatusCode);

        var withTenant = await superAdmin.PostAsJsonAsync("/api/users/invite",
            new
            {
                email = "super.invited@tenant-a.com",
                role_ids = new[] { _factory.TenantOperatorRoleId },
                tenant_id = _factory.TenantId
            });
        withTenant.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Expired_invitation_token_is_rejected()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();

        const string email = "expired.user@tenant-a.com";
        const string rawToken = "expired-test-token-value";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = _factory.TenantId,
                Email = email,
                Status = UserStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);
            db.InvitationTokens.Add(new InvitationToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = TokenHasher.Sha256(rawToken),
                InvitedBy = _factory.TenantAdminUserId,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1)
            });
            await db.SaveChangesAsync();
        }

        var activate = await _factory.AnonymousClient.PostAsJsonAsync("/api/auth/activate",
            new { token = rawToken, password = "SecurePass!123" });
        Assert.Equal(HttpStatusCode.BadRequest, activate.StatusCode);

        var body = await activate.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("INVITATION_TOKEN_INVALID", body?.Code);
    }

    private record UserSummary(Guid Id, string Email, string Status);
    private record ErrorResponse([property: System.Text.Json.Serialization.JsonPropertyName("code")] string Code);
}
