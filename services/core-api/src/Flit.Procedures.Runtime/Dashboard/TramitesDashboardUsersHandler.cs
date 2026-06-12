using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Shared.Auth;
using Flit.Procedures.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Runtime.Dashboard;

public sealed class TramitesDashboardUsersHandler(
    ProceduresDbContext proceduresDb,
    IdentityDbContext identityDb)
{
    public async Task<IResult> HandleTopAsync(
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

        var counts = await TramitesDashboardQuery.Apply(proceduresDb, effectiveTenantId, range!)
            .GroupBy(i => i.CreatedBy)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(ct);

        var userIds = counts.Select(c => c.UserId).ToList();
        var users = await identityDb.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var items = counts.Select(c =>
        {
            users.TryGetValue(c.UserId, out var u);
            return new DashboardUserItemDto(
                c.UserId,
                u?.Email ?? c.UserId.ToString(),
                u?.Email ?? "",
                c.Count);
        }).ToList();

        return Results.Ok(new DashboardUsersTopResponse(items));
    }

    public async Task<IResult> HandleSearchAsync(
        CurrentUser user,
        string? q,
        Guid? tenantId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        if (TramitesDashboardQuery.TryResolveTenant(user, tenantId, out var effectiveTenantId) is { } tenantError)
        {
            return tenantError;
        }

        if (effectiveTenantId is not { } tid)
        {
            return Results.Json(
                new { code = "VALIDATION_ERROR", message = "tenantId is required for global search." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = identityDb.Users.AsNoTracking().Where(u => u.TenantId == tid);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(u => u.Email.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new DashboardUserSearchItemDto(u.Id, u.Email))
            .ToListAsync(ct);

        return Results.Ok(new DashboardUsersSearchResponse(items, totalCount, page, pageSize));
    }

    public async Task<IResult> HandleStatsAsync(
        CurrentUser user,
        Guid userId,
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

        var count = await TramitesDashboardQuery.Apply(proceduresDb, effectiveTenantId, range!)
            .CountAsync(i => i.CreatedBy == userId, ct);

        return Results.Ok(new DashboardUserStatsResponse(userId, count));
    }
}
