using System.Net;
using System.Net.Http.Json;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.OT.Infrastructure.Persistence;
using Flit.OT.Infrastructure.Persistence.Entities;
using Flit.OT.Shared.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Ot;

public class OtIndexTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public OtIndexTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task SuperAdmin_can_query_ot_index()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await _factory.LoginAsSuperAdminAsync();
        var response = await client.GetAsync("/api/v1/admin/ot/index?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Tenant_admin_gets_forbidden_on_ot_index()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await _factory.LoginAsTenantAdminAsync();
        var response = await client.GetAsync("/api/v1/admin/ot/index");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Super_admin_gets_paginated_index_with_filters()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var seeded = await SeedOtProfilesAsync();
        var target = seeded[0];
        var client = await _factory.LoginAsSuperAdminAsync();

        var response = await client.GetAsync("/api/v1/admin/ot/index?page=1&pageSize=20&sort=createdAt:desc");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IndexResponse>();
        Assert.NotNull(body);
        Assert.True(body.TotalCount >= 2);

        var byDivipol = await client.GetAsync(
            $"/api/v1/admin/ot/index?divipol={Uri.EscapeDataString(target.DivipolCode[..6])}");
        byDivipol.EnsureSuccessStatusCode();
        var divipolBody = await byDivipol.Content.ReadFromJsonAsync<IndexResponse>();
        Assert.Contains(divipolBody!.Items, i => i.Id == target.Id);

        var byName = await client.GetAsync(
            $"/api/v1/admin/ot/index?name={Uri.EscapeDataString("Bogotá")}");
        byName.EnsureSuccessStatusCode();
        var nameBody = await byName.Content.ReadFromJsonAsync<IndexResponse>();
        Assert.Contains(nameBody!.Items, i => i.Id == target.Id);
    }

    private async Task<IReadOnlyList<SeededOt>> SeedOtProfilesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var otDb = scope.ServiceProvider.GetRequiredService<OtDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var now = DateTimeOffset.UtcNow;
        var seeded = new List<SeededOt>();

        for (var i = 0; i < 2; i++)
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = $"OT Tenant {suffix}-{i}",
                Slug = $"ot-{suffix}-{i}",
                IsActive = true,
                CreatedAt = now.AddMinutes(-i)
            };
            identityDb.Tenants.Add(tenant);

            var profile = new OtProfile
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                DivipolCode = $"110010{i}{suffix[..4]}",
                DisplayName = i == 0 ? $"OT Bogotá {suffix}" : $"OT Medellín {suffix}",
                Status = OtStatus.Active,
                IntegrationMode = IntegrationMode.Dashboard,
                CreatedAt = now.AddHours(-i),
                UpdatedAt = now.AddHours(-i)
            };
            otDb.OtProfiles.Add(profile);
            seeded.Add(new SeededOt(profile.Id, profile.DivipolCode, profile.DisplayName));
        }

        await identityDb.SaveChangesAsync();
        await otDb.SaveChangesAsync();
        return seeded;
    }

    private sealed record SeededOt(Guid Id, string DivipolCode, string DisplayName);

    private sealed record IndexResponse(
        List<IndexItem> Items,
        int TotalCount,
        int Page,
        int PageSize);

    private sealed record IndexItem(
        Guid Id,
        Guid TenantId,
        string DivipolCode,
        string DisplayName,
        OtStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
