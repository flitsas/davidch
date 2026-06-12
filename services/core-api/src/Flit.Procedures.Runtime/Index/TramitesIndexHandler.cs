using Flit.Identity.Shared.Auth;
using Flit.OT.Infrastructure.Persistence;
using Flit.Procedures.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Runtime.Index;

public sealed class TramitesIndexHandler(
    ProceduresDbContext db,
    OtDbContext otDb)
{
    public async Task<IResult> HandleAsync(
        CurrentUser user,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.ProcedureInstances.AsNoTracking();
        if (!user.IsSuperAdmin)
        {
            if (user.TenantId is not { } tenantId)
            {
                return Results.Json(
                    new { code = "FORBIDDEN" },
                    statusCode: StatusCodes.Status403Forbidden);
            }

            query = query.Where(i => i.TenantId == tenantId);
        }

        var totalCount = await query.CountAsync(ct);
        var instances = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(i => i.ProcedureType)
            .ToListAsync(ct);

        var divipolCodes = instances.Select(i => i.OtDivipolCode).Distinct().ToList();
        var otNames = await otDb.OtProfiles
            .AsNoTracking()
            .Where(o => divipolCodes.Contains(o.DivipolCode))
            .ToDictionaryAsync(o => o.DivipolCode, o => o.DisplayName, ct);

        var items = instances.Select(i => new TramiteIndexItem(
            i.Id,
            i.Id.ToString()[..8].ToUpperInvariant(),
            i.ProcedureType.Name,
            i.ProcedureTypeCode,
            otNames.GetValueOrDefault(i.OtDivipolCode, i.OtDivipolCode),
            i.OtDivipolCode,
            i.VehicleQueryValue,
            i.Status.ToString(),
            i.CreatedAt,
            i.CreatedBy)).ToList();

        return Results.Ok(new TramitesIndexResponse(items, totalCount, page, pageSize));
    }
}
