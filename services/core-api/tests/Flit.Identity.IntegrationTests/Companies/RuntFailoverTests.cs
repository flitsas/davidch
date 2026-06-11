using System.Net;
using System.Net.Http.Json;
using Flit.Companies.Infrastructure.Persistence;
using Flit.Companies.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Companies;

public class RuntFailoverTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public RuntFailoverTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Runt_failover_uses_secondary_when_primary_stub_times_out()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var companyId = await CreateCompanyForTenantAsync();
        var superAdmin = await _factory.LoginAsSuperAdminAsync();
        var put = await superAdmin.PutAsJsonAsync(
            $"/api/v1/admin/companies/{companyId}/config/runt",
            new
            {
                primaryProvider = 1,
                secondaryProvider = 0,
                failoverTimeoutMs = 500,
                providerCredentials = "{}"
            });
        put.EnsureSuccessStatusCode();

        var client = await CreateTenantClientForCompanyAsync(companyId);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/runt/placa?q=ABC123");
        request.Headers.Add("X-Runt-Stub-Mode", "timeout");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<RuntResponse>();
        Assert.Equal("Verifik", body!.Provider);
        Assert.Equal("timeout", body.FailoverReason);
    }

    private async Task<Guid> CreateCompanyForTenantAsync()
    {
        var client = await _factory.LoginAsSuperAdminAsync();
        var slug = $"runt-{Guid.NewGuid():N}"[..16];
        var response = await client.PostAsJsonAsync("/api/v1/admin/companies", new
        {
            mode = "create",
            nit = $"900{Guid.NewGuid():N}"[..12],
            legal_name = "RUNT Test SAS",
            slug,
            status = "Active"
        });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        return created!.Id;
    }

    private async Task<HttpClient> CreateTenantClientForCompanyAsync(Guid companyId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CompaniesDbContext>();
        var tenantId = await db.Companies
            .Where(c => c.Id == companyId)
            .Select(c => c.TenantId)
            .SingleAsync();

        var identityDb = scope.ServiceProvider.GetRequiredService<Flit.Identity.Infrastructure.Persistence.IdentityDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<Flit.Identity.Infrastructure.Security.IPasswordHasher>();
        var email = $"runt-admin-{Guid.NewGuid():N}"[..24] + "@runt.test";
        var password = "SecurePass!123";
        var user = new Flit.Identity.Infrastructure.Persistence.Entities.User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            PasswordHash = hasher.Hash(password),
            Status = Flit.Identity.Shared.Domain.UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        identityDb.Users.Add(user);
        await identityDb.SaveChangesAsync();

        var http = _factory.CreateClient(new() { HandleCookies = true });
        var login = await http.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        return http;
    }

    private sealed record CreatedResponse(Guid Id, Guid TenantId);
    private sealed record RuntResponse(string Provider, string? FailoverReason);
}
