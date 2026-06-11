using Flit.OT.Infrastructure.Persistence;
using Flit.OT.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.OT.Admin.Index;

public sealed class OtIndexHandler(OtDbContext db)
{
    public async Task<IResult> HandleAsync(OtIndexRequest request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.OtProfiles.AsNoTracking();

        if (request.Id is { } id)
        {
            query = query.Where(o => o.Id == id);
        }

        if (!string.IsNullOrWhiteSpace(request.Divipol))
        {
            var divipol = request.Divipol.Trim();
            query = query.Where(o => EF.Functions.ILike(o.DivipolCode, $"%{divipol}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(o => EF.Functions.ILike(o.DisplayName, $"%{name}%"));
        }

        if (request.AuditFrom is { } auditFrom)
        {
            query = query.Where(o => o.UpdatedAt >= auditFrom);
        }

        if (request.AuditTo is { } auditTo)
        {
            query = query.Where(o => o.UpdatedAt <= auditTo);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await ApplySort(query, request.Sort ?? "createdAt:desc")
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OtIndexItem(
                o.Id,
                o.TenantId,
                o.DivipolCode,
                o.DisplayName,
                o.Status,
                o.CreatedAt,
                o.UpdatedAt))
            .ToListAsync(ct);

        return Results.Ok(new OtIndexResponse(items, totalCount, page, pageSize));
    }

    private static IQueryable<OtProfile> ApplySort(IQueryable<OtProfile> query, string sort)
    {
        var (field, descending) = ParseSort(sort);

        return field switch
        {
            "updatedat" => descending
                ? query.OrderByDescending(o => o.UpdatedAt)
                : query.OrderBy(o => o.UpdatedAt),
            "displayname" or "name" => descending
                ? query.OrderByDescending(o => o.DisplayName)
                : query.OrderBy(o => o.DisplayName),
            "divipol" or "divipolcode" => descending
                ? query.OrderByDescending(o => o.DivipolCode)
                : query.OrderBy(o => o.DivipolCode),
            _ => descending
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt),
        };
    }

    private static (string Field, bool Descending) ParseSort(string sort)
    {
        var parts = sort.Split(':', 2, StringSplitOptions.TrimEntries);
        var field = parts[0].Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();
        var descending = parts.Length < 2
            || parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        return (field, descending);
    }
}
