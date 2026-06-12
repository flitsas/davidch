using Flit.Companies.Infrastructure.Persistence;
using Flit.OT.Infrastructure.Persistence;
using Flit.OT.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Runtime.Ot;

public sealed record TrafficAuthorityDto(
    string DivipolCode,
    string DisplayName,
    Guid OtTenantId);

public sealed class TrafficAuthorityPicker(
    CompaniesDbContext companiesDb,
    OtDbContext otDb)
{
    public async Task<IReadOnlyList<TrafficAuthorityDto>> ListForTenantAsync(
        Guid tenantId,
        CancellationToken ct)
    {
        var company = await companiesDb.Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (company is null)
        {
            return [];
        }

        var enabledCodes = await companiesDb.CompanyTrafficAuthorityMatrix
            .AsNoTracking()
            .Where(m => m.CompanyId == company.Id && m.IsEnabled)
            .Select(m => m.AuthorityCode)
            .ToListAsync(ct);

        if (enabledCodes.Count == 0)
        {
            return [];
        }

        var catalogByCode = await companiesDb.TrafficAuthorities
            .AsNoTracking()
            .Where(a => enabledCodes.Contains(a.Code))
            .ToDictionaryAsync(a => a.Code, ct);

        var otProfiles = await otDb.OtProfiles
            .AsNoTracking()
            .Where(o => enabledCodes.Contains(o.DivipolCode) && o.Status == OtStatus.Active)
            .ToListAsync(ct);

        return otProfiles
            .Select(o => new TrafficAuthorityDto(
                o.DivipolCode,
                catalogByCode.TryGetValue(o.DivipolCode, out var catalog)
                    ? catalog.Name
                    : o.DisplayName,
                o.TenantId))
            .OrderBy(o => o.DisplayName)
            .ToList();
    }

    public async Task<TrafficAuthorityDto?> ResolveAsync(
        Guid tenantId,
        string divipolCode,
        CancellationToken ct)
    {
        var items = await ListForTenantAsync(tenantId, ct);
        return items.SingleOrDefault(i =>
            string.Equals(i.DivipolCode, divipolCode, StringComparison.Ordinal));
    }
}
