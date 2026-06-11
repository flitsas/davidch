using Flit.Companies.Infrastructure.Persistence;
using Flit.Companies.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Companies.Admin.Index;

public sealed class CompanyIndexHandler(CompaniesDbContext db)
{
    public async Task<IResult> HandleAsync(CompanyIndexRequest request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.Companies.AsNoTracking();

        if (request.Id is { } id)
        {
            query = query.Where(c => c.Id == id);
        }

        if (!string.IsNullOrWhiteSpace(request.Nit))
        {
            var nit = request.Nit.Trim();
            query = query.Where(c => EF.Functions.ILike(c.Nit, $"%{nit}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(c => EF.Functions.ILike(c.LegalName, $"%{name}%"));
        }

        if (request.AuditFrom is { } auditFrom)
        {
            query = query.Where(c => c.UpdatedAt >= auditFrom);
        }

        if (request.AuditTo is { } auditTo)
        {
            query = query.Where(c => c.UpdatedAt <= auditTo);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await ApplySort(query, request.Sort ?? "createdAt:desc")
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CompanyIndexItem(
                c.Id,
                c.TenantId,
                c.Nit,
                c.LegalName,
                c.Status,
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync(ct);

        return Results.Ok(new CompanyIndexResponse(items, totalCount, page, pageSize));
    }

    private static IQueryable<Company> ApplySort(
        IQueryable<Company> query,
        string sort)
    {
        var (field, descending) = ParseSort(sort);

        return field switch
        {
            "updatedat" => descending
                ? query.OrderByDescending(c => c.UpdatedAt)
                : query.OrderBy(c => c.UpdatedAt),
            "legalname" or "name" => descending
                ? query.OrderByDescending(c => c.LegalName)
                : query.OrderBy(c => c.LegalName),
            "nit" => descending
                ? query.OrderByDescending(c => c.Nit)
                : query.OrderBy(c => c.Nit),
            _ => descending
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt),
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
