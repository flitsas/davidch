using Flit.Identity.Shared.Auth;
using Flit.Procedures.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Runtime.Dashboard;

public sealed class TramitesDashboardSummaryHandler(ProceduresDbContext db)
{
    private static readonly (string Key, string Label)[] CategoryMeta =
    [
        (ProcedureCategoryClassifier.Matriculas, "Matrículas"),
        (ProcedureCategoryClassifier.Traspasos, "Traspasos"),
        (ProcedureCategoryClassifier.Otros, "Otros trámites"),
    ];

    public async Task<IResult> HandleAsync(
        CurrentUser user,
        string? from,
        string? to,
        Guid? tenantId,
        CancellationToken ct)
    {
        if (TramitesDashboardDateRange.TryParse(from, to, out var range) is { } rangeError)
        {
            return rangeError;
        }

        if (TramitesDashboardQuery.TryResolveTenant(user, tenantId, out var effectiveTenantId) is { } tenantError)
        {
            return tenantError;
        }

        var codes = await TramitesDashboardQuery.Apply(db, effectiveTenantId, range!)
            .Select(i => i.ProcedureTypeCode)
            .ToListAsync(ct);

        var total = codes.Count;
        var categories = CategoryMeta.Select(meta =>
        {
            var count = codes.Count(c => ProcedureCategoryClassifier.MatchesCategory(c, meta.Key));
            var percent = total == 0 ? 0 : Math.Round(count * 100.0 / total, 1);
            return new DashboardCategoryDto(meta.Key, meta.Label, count, percent);
        }).ToList();

        return Results.Ok(new DashboardSummaryResponse(
            range!.From.ToString("yyyy-MM-dd"),
            range.To.ToString("yyyy-MM-dd"),
            total,
            categories));
    }
}
