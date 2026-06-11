using Flit.Procedures.Infrastructure.Persistence.Entities;
using Flit.Procedures.Shared.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProceduresDbContext = Flit.Procedures.Infrastructure.Persistence.ProceduresDbContext;

namespace Flit.Procedures.Admin.Index;

public sealed class ProcedureTypeIndexHandler(ProceduresDbContext db)
{
    public async Task<IResult> HandleAsync(ProcedureTypeIndexRequest request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.ProcedureTypes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{name}%")
                || EF.Functions.ILike(p.Code, $"%{name}%"));
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(p => p.IsActive == isActive);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await ApplySort(query, request.Sort ?? "updatedAt:desc")
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProcedureTypeIndexItem(
                p.Id,
                p.Name,
                p.Code,
                p.VehicleQueryMode,
                p.IsActive,
                p.Actors.Count,
                p.Documents.Count,
                p.UpdatedAt))
            .ToListAsync(ct);

        return Results.Ok(new ProcedureTypeIndexResponse(items, totalCount, page, pageSize));
    }

    private static IQueryable<ProcedureType> ApplySort(IQueryable<ProcedureType> query, string sort)
    {
        var (field, descending) = ParseSort(sort);

        return field switch
        {
            "name" => descending
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
            "code" => descending
                ? query.OrderByDescending(p => p.Code)
                : query.OrderBy(p => p.Code),
            "createdat" => descending
                ? query.OrderByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.CreatedAt),
            _ => descending
                ? query.OrderByDescending(p => p.UpdatedAt)
                : query.OrderBy(p => p.UpdatedAt),
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
