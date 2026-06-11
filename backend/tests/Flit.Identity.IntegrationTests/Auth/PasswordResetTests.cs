using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Auth;

public class PasswordResetTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public PasswordResetTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Forgot_password_always_returns_200_even_for_unknown_email()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var res = await _factory.AnonymousClient.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "nobody@example.com" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Reset_password_bumps_token_version_and_revokes_sessions()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var user = await _factory.CreateActiveUserAsync();
        var cookies = await _factory.LoginAndGetCookiesAsync(user.Email, user.Password);

        var tokenVersionBefore = await GetTokenVersionAsync(user.Id);
        var raw = await _factory.CreateSelfServiceResetTokenAsync(user.Email);

        var reset = await _factory.AnonymousClient.PostAsJsonAsync("/api/auth/reset-password",
            new { token = raw, new_password = "NewPass!456" });
        reset.EnsureSuccessStatusCode();

        var tokenVersionAfter = await GetTokenVersionAsync(user.Id);
        Assert.True(tokenVersionAfter > tokenVersionBefore);

        var me = await _factory.SendWithCookiesAsync("/api/auth/me", cookies);
        Assert.Equal(HttpStatusCode.Forbidden, me.StatusCode);
        Assert.Equal("SESSION_REVOKED", await me.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task Force_reset_sends_email()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var user = await _factory.CreateActiveUserAsync();
        var admin = await _factory.LoginAsTenantAdminAsync();

        var response = await admin.PostAsync($"/api/users/{user.Id}/force-reset", null);
        response.EnsureSuccessStatusCode();

        var token = _factory.GetLatestResetToken(user.Email);
        Assert.NotNull(token);
    }

    private async Task<int> GetTokenVersionAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.TokenVersion)
            .SingleAsync();
    }

    private record ErrorResponse([property: JsonPropertyName("code")] string Code);
}

internal static class HttpResponseExtensions
{
    public static async Task<string?> ReadErrorCodeAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        return body?.Code;
    }

    private record ErrorBody([property: JsonPropertyName("code")] string Code);
}
