using System.Net;
using System.Net.Http.Json;
using Flit.Companies.Infrastructure.Persistence;
using Flit.Companies.Infrastructure.Persistence.Entities;
using Flit.Companies.Shared.Domain;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Companies;

public class CompaniesIndexTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public CompaniesIndexTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Non_super_admin_gets_forbidden()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var client = await _factory.LoginAsTenantAdminAsync();
        var response = await client.GetAsync("/api/v1/admin/companies/index");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("FORBIDDEN", body?.Code);
    }

    [Fact]
    public async Task Super_admin_gets_paginated_index_sorted_by_created_at_desc()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await SeedCompaniesAsync();

        var client = await _factory.LoginAsSuperAdminAsync();
        var response = await client.GetAsync("/api/v1/admin/companies/index?page=1&pageSize=20&sort=createdAt:desc");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<IndexResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body.Page);
        Assert.Equal(20, body.PageSize);
        Assert.True(body.TotalCount >= 3);
        Assert.NotEmpty(body.Items);

        for (var i = 1; i < body.Items.Count; i++)
        {
            Assert.True(body.Items[i - 1].CreatedAt >= body.Items[i].CreatedAt);
        }
    }

    [Fact]
    public async Task Super_admin_can_filter_by_nit_name_id_and_audit_range()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var seeded = await SeedCompaniesAsync();
        var target = seeded[1];
        var client = await _factory.LoginAsSuperAdminAsync();

        var byNit = await client.GetAsync($"/api/v1/admin/companies/index?nit={Uri.EscapeDataString(target.Nit[..6])}");
        byNit.EnsureSuccessStatusCode();
        var nitBody = await byNit.Content.ReadFromJsonAsync<IndexResponse>();
        Assert.Contains(nitBody!.Items, i => i.Id == target.Id);

        var byName = await client.GetAsync($"/api/v1/admin/companies/index?name={Uri.EscapeDataString("Alpha")}");
        byName.EnsureSuccessStatusCode();
        var nameBody = await byName.Content.ReadFromJsonAsync<IndexResponse>();
        Assert.All(nameBody!.Items, i => Assert.Contains("Alpha", i.LegalName, StringComparison.OrdinalIgnoreCase));

        var byId = await client.GetAsync($"/api/v1/admin/companies/index?id={target.Id}");
        byId.EnsureSuccessStatusCode();
        var idBody = await byId.Content.ReadFromJsonAsync<IndexResponse>();
        Assert.Single(idBody!.Items);
        Assert.Equal(target.Id, idBody.Items[0].Id);

        var auditFrom = target.UpdatedAt.AddDays(-1).ToString("O");
        var auditTo = target.UpdatedAt.AddDays(1).ToString("O");
        var byAudit = await client.GetAsync(
            $"/api/v1/admin/companies/index?auditFrom={Uri.EscapeDataString(auditFrom)}&auditTo={Uri.EscapeDataString(auditTo)}");
        byAudit.EnsureSuccessStatusCode();
        var auditBody = await byAudit.Content.ReadFromJsonAsync<IndexResponse>();
        Assert.Contains(auditBody!.Items, i => i.Id == target.Id);
    }

    private async Task<IReadOnlyList<SeededCompany>> SeedCompaniesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var companiesDb = scope.ServiceProvider.GetRequiredService<CompaniesDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var now = DateTimeOffset.UtcNow;
        var seeded = new List<SeededCompany>();

        for (var i = 0; i < 3; i++)
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = $"Company Tenant {suffix}-{i}",
                Slug = $"co-{suffix}-{i}",
                IsActive = true,
                CreatedAt = now.AddMinutes(-i)
            };
            identityDb.Tenants.Add(tenant);

            var company = new Company
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Nit = $"900{suffix}{i}",
                LegalName = i == 1 ? $"Alpha Renting {suffix}" : $"Beta Motors {suffix}-{i}",
                Status = CompanyStatus.Active,
                CreatedAt = now.AddHours(-i),
                UpdatedAt = now.AddHours(-i)
            };
            companiesDb.Companies.Add(company);
            seeded.Add(new SeededCompany(company.Id, company.Nit, company.LegalName, company.UpdatedAt));
        }

        await identityDb.SaveChangesAsync();
        await companiesDb.SaveChangesAsync();
        return seeded;
    }

    private sealed record SeededCompany(Guid Id, string Nit, string LegalName, DateTimeOffset UpdatedAt);

    private sealed record ErrorResponse(string Code);

    private sealed record IndexResponse(
        List<IndexItem> Items,
        int TotalCount,
        int Page,
        int PageSize);

    private sealed record IndexItem(
        Guid Id,
        Guid TenantId,
        string Nit,
        string LegalName,
        CompanyStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
