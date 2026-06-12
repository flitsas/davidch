using Flit.Identity.Shared.Auth;
using Flit.Procedures.Infrastructure.Persistence;
using Flit.Procedures.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Runtime.Dashboard;

public static class TramitesDashboardQuery
{
    public static IResult? TryResolveTenant(
        CurrentUser user,
        Guid? tenantId,
        out Guid? effectiveTenantId)
    {
        effectiveTenantId = null;
        if (user.IsSuperAdmin)
        {
            effectiveTenantId = tenantId;
            return null;
        }

        if (user.TenantId is not { } tid)
        {
            return Results.Json(new { code = "FORBIDDEN" }, statusCode: StatusCodes.Status403Forbidden);
        }

        effectiveTenantId = tid;
        return null;
    }

    public static IQueryable<ProcedureInstance> Apply(
        ProceduresDbContext db,
        Guid? tenantId,
        TramitesDashboardDateRange range)
    {
        var query = db.ProcedureInstances.AsNoTracking()
            .Where(i => i.CreatedAt >= range.FromUtc && i.CreatedAt <= range.ToUtc);

        if (tenantId is { } tid)
        {
            query = query.Where(i => i.TenantId == tid);
        }

        return query;
    }
}
