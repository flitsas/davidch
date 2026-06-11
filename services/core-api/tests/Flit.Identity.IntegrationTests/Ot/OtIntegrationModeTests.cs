using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flit.OT.Infrastructure.Persistence;
using Flit.OT.Shared;
using Flit.OT.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Ot;

public class OtIntegrationModeTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;
    private static HttpClient? _superAdminClient;
    private static readonly SemaphoreSlim LoginGate = new(1, 1);
    private static readonly JsonSerializerOptions OtJson = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public OtIntegrationModeTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task SuperAdmin_can_get_and_put_integration_mode()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var otId = await GetTenantAOtProfileIdAsync();
        var client = await SuperAdminAsync();

        var get = await client.GetAsync($"/api/v1/admin/ot/{otId}/config/integration");
        get.EnsureSuccessStatusCode();
        var current = await get.Content.ReadFromJsonAsync<IntegrationResponse>(OtJson);
        Assert.Equal(IntegrationMode.Dashboard, current!.IntegrationMode);

        var put = await client.PutAsJsonAsync(
            $"/api/v1/admin/ot/{otId}/config/integration",
            new { integration_mode = "Qx" });
        put.EnsureSuccessStatusCode();
        var updated = await put.Content.ReadFromJsonAsync<IntegrationResponse>(OtJson);
        Assert.Equal(IntegrationMode.Qx, updated!.IntegrationMode);
    }

    [Fact]
    public async Task Tenant_admin_can_get_and_put_settings_integration_mode()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsTenantAdminAsync();

        var get = await client.GetAsync("/api/v1/ot/settings/config/integration");
        get.EnsureSuccessStatusCode();

        var put = await client.PutAsJsonAsync(
            "/api/v1/ot/settings/config/integration",
            new { integration_mode = "Dashboard" });
        put.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Integration_mode_service_returns_persisted_mode()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var otId = await GetTenantAOtProfileIdAsync();
        var client = await SuperAdminAsync();

        await client.PutAsJsonAsync(
            $"/api/v1/admin/ot/{otId}/config/integration",
            new { integration_mode = "Qx" });

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOtIntegrationModeService>();
        var mode = await service.GetModeAsync(_factory.TenantId, CancellationToken.None);
        Assert.Equal(IntegrationMode.Qx, mode);
    }

    private async Task<Guid> GetTenantAOtProfileIdAsync()
    {
        await _factory.EnsureTenantSeededAsync();
        using var scope = _factory.Services.CreateScope();
        var otDb = scope.ServiceProvider.GetRequiredService<OtDbContext>();
        var profile = await otDb.OtProfiles.AsNoTracking()
            .SingleAsync(o => o.TenantId == _factory.TenantId);
        return profile.Id;
    }

    private async Task<HttpClient> SuperAdminAsync()
    {
        if (_superAdminClient is not null)
        {
            return _superAdminClient;
        }

        await LoginGate.WaitAsync();
        try
        {
            _superAdminClient ??= await _factory.LoginAsSuperAdminAsync();
            return _superAdminClient;
        }
        finally
        {
            LoginGate.Release();
        }
    }

    private sealed class IntegrationResponse
    {
        [JsonPropertyName("integration_mode")]
        public IntegrationMode IntegrationMode { get; init; }
    }
}
