using Flit.Companies.Infrastructure.Persistence;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Companies.Admin.Config;

public sealed class CompanyTrafficAuthoritiesHandler(CompaniesDbContext db)
{
    public async Task<IResult> GetAsync(
        Guid companyId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        if (!await db.Companies.AnyAsync(c => c.Id == companyId, ct))
        {
            return NotFound();
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query =
            from matrix in db.CompanyTrafficAuthorityMatrix.AsNoTracking()
            join authority in db.TrafficAuthorities.AsNoTracking()
                on matrix.AuthorityCode equals authority.Code
            where matrix.CompanyId == companyId
            orderby authority.Name
            select new TrafficAuthorityItem(
                matrix.AuthorityCode,
                authority.Name,
                authority.Region,
                matrix.IsEnabled);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(new TrafficAuthoritiesResponse(items, totalCount, page, pageSize));
    }

    public async Task<IResult> PatchAsync(
        Guid companyId,
        TrafficAuthoritiesPatchRequest request,
        CancellationToken ct)
    {
        if (!await db.Companies.AnyAsync(c => c.Id == companyId, ct))
        {
            return NotFound();
        }

        if (request.Updates.Count == 0)
        {
            return ValidationError("At least one update is required.");
        }

        var codes = request.Updates.Select(u => u.AuthorityCode).ToList();
        var rows = await db.CompanyTrafficAuthorityMatrix
            .Where(m => m.CompanyId == companyId && codes.Contains(m.AuthorityCode))
            .ToListAsync(ct);

        if (rows.Count != request.Updates.Count)
        {
            return ValidationError("One or more authority codes are invalid.");
        }

        var updatesByCode = request.Updates.ToDictionary(u => u.AuthorityCode);
        foreach (var row in rows)
        {
            row.IsEnabled = updatesByCode[row.AuthorityCode].IsEnabled;
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static IResult NotFound() =>
        Results.Json(new { code = ApiErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound);

    private static IResult ValidationError(string message) =>
        Results.Json(new { code = ApiErrorCodes.ValidationError, message }, statusCode: StatusCodes.Status400BadRequest);
}
