using Flit.Identity.Shared.Auth;
using Flit.Procedures.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Runtime.Dashboard;

public sealed class TramitesDashboardDetailHandler(ProceduresDbContext db)
{
    public async Task<IResult> HandleAsync(
        CurrentUser user,
        string category,
        string? from,
        string? to,
        Guid? tenantId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return Results.Json(
                new { code = "VALIDATION_ERROR", message = "category is required." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (TramitesDashboardDateRange.TryParse(from, to, out var range) is { } rangeError)
        {
            return rangeError;
        }

        if (TramitesDashboardQuery.TryResolveTenant(user, tenantId, out var effectiveTenantId) is { } tenantError)
        {
            return tenantError;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var instances = await TramitesDashboardQuery.Apply(db, effectiveTenantId, range!)
            .Include(i => i.Actors)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        var filtered = instances
            .Where(i => ProcedureCategoryClassifier.MatchesCategory(i.ProcedureTypeCode, category))
            .ToList();

        var totalCount = filtered.Count;
        var pageItems = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToRow)
            .ToList();

        return Results.Ok(new DashboardDetailResponse(pageItems, totalCount, page, pageSize));
    }

    internal static DashboardDetailRowDto ToRow(
        Flit.Procedures.Infrastructure.Persistence.Entities.ProcedureInstance instance) =>
        new(
            instance.Id.ToString()[..8].ToUpperInvariant(),
            instance.Id,
            instance.CreatedAt,
            instance.Status.ToString(),
            instance.VehicleQueryValue,
            OwnerNameResolver.Resolve(instance.Actors),
            instance.UpdatedAt);
}
