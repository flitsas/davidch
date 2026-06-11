using Flit.Companies.Infrastructure.Persistence;
using Flit.Companies.Infrastructure.Persistence.Entities;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Companies.Admin.Config;

public sealed class CompanyExceptionsHandler(
    CompaniesDbContext companiesDb,
    IdentityDbContext identityDb)
{
    public async Task<IResult> AddBatchAsync(
        Guid companyId,
        ExceptionsBatchRequest request,
        CancellationToken ct)
    {
        var company = await companiesDb.Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == companyId, ct);

        if (company is null)
        {
            return NotFound();
        }

        if (request.UserIds.Count == 0)
        {
            return ValidationError("At least one user id is required.");
        }

        var validUserIds = await identityDb.Users
            .AsNoTracking()
            .Where(u => u.TenantId == company.TenantId && request.UserIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (validUserIds.Count != request.UserIds.Count)
        {
            return ValidationError("All users must belong to the company tenant.");
        }

        var existing = await companiesDb.TenantUserExceptions
            .Where(e => e.TenantId == company.TenantId && request.UserIds.Contains(e.UserId))
            .Select(e => e.UserId)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var userId in validUserIds.Where(id => !existing.Contains(id)))
        {
            companiesDb.TenantUserExceptions.Add(new TenantUserException
            {
                TenantId = company.TenantId,
                UserId = userId,
                CreatedAt = now
            });
        }

        await companiesDb.SaveChangesAsync(ct);
        return Results.Ok(new { added = validUserIds.Count - existing.Count });
    }

    public async Task<IResult> DeleteBatchAsync(
        Guid companyId,
        ExceptionsBatchRequest request,
        CancellationToken ct)
    {
        var company = await companiesDb.Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == companyId, ct);

        if (company is null)
        {
            return NotFound();
        }

        if (request.UserIds.Count == 0)
        {
            return ValidationError("At least one user id is required.");
        }

        var rows = await companiesDb.TenantUserExceptions
            .Where(e => e.TenantId == company.TenantId && request.UserIds.Contains(e.UserId))
            .ToListAsync(ct);

        companiesDb.TenantUserExceptions.RemoveRange(rows);
        await companiesDb.SaveChangesAsync(ct);
        return Results.Ok(new { removed = rows.Count });
    }

    private static IResult NotFound() =>
        Results.Json(new { code = ApiErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound);

    private static IResult ValidationError(string message) =>
        Results.Json(new { code = ApiErrorCodes.ValidationError, message }, statusCode: StatusCodes.Status400BadRequest);
}
