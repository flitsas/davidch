using Flit.Companies.Infrastructure.Persistence.Entities;
using Flit.Companies.Shared.Domain;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace Flit.Companies.Infrastructure.Persistence.Seed;

/// <summary>
/// Links dev tenant-a to a company profile and enables Bogotá OT for tramites flows.
/// </summary>
public static class DevCompanySeeder
{
    public const string DemoCompanyNit = "900123456-1";
    public const string DemoAuthorityCode = "11001000";

    public static async Task SeedAsync(
        CompaniesDbContext companiesDb,
        IdentityDbContext identityDb,
        CancellationToken ct = default)
    {
        var tenant = await identityDb.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Slug == DevTenantSeeder.TenantSlug, ct);

        if (tenant is null)
        {
            return;
        }

        if (await companiesDb.Companies.AnyAsync(c => c.TenantId == tenant.Id, ct))
        {
            await EnsureAuthorityEnabledAsync(companiesDb, tenant.Id, ct);
            return;
        }

        var authorityCodes = await companiesDb.TrafficAuthorities
            .AsNoTracking()
            .Select(a => a.Code)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var company = new Company
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Nit = DemoCompanyNit,
            LegalName = "Empresa Demo Tenant A",
            Status = CompanyStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };

        CompanyDefaultConfigFactory.SeedDefaults(company, authorityCodes, now);
        foreach (var row in company.TrafficAuthorityMatrix)
        {
            row.IsEnabled = row.AuthorityCode == DemoAuthorityCode;
        }

        companiesDb.Companies.Add(company);
        await companiesDb.SaveChangesAsync(ct);
    }

    private static async Task EnsureAuthorityEnabledAsync(
        CompaniesDbContext companiesDb,
        Guid tenantId,
        CancellationToken ct)
    {
        var company = await companiesDb.Companies
            .Include(c => c.TrafficAuthorityMatrix)
            .SingleOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (company is null)
        {
            return;
        }

        var row = company.TrafficAuthorityMatrix
            .SingleOrDefault(m => m.AuthorityCode == DemoAuthorityCode);

        if (row is null || row.IsEnabled)
        {
            return;
        }

        row.IsEnabled = true;
        await companiesDb.SaveChangesAsync(ct);
    }
}
