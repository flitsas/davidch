using System.Security.Claims;
using Flit.Companies.Infrastructure.Persistence;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Companies.Runt;

public sealed class RuntQueryHandler(
    CompaniesDbContext companiesDb,
    RuntProxy proxy)
{
    public async Task<IResult> HandleAsync(
        ClaimsPrincipal user,
        string typeSegment,
        string query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return ValidationError("Query parameter q is required.");
        }

        if (!Enum.TryParse<RuntQueryType>(typeSegment, ignoreCase: true, out var queryType))
        {
            return ValidationError("Type must be placa, conductor, or vin.");
        }

        var tenantClaim = user.FindFirstValue("tenant_id");
        if (tenantClaim is null || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            return Forbidden();
        }

        var company = await companiesDb.Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (company is null)
        {
            return NotFound();
        }

        var runtConfig = await companiesDb.CompanyRuntConfigs
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.CompanyId == company.Id, ct);

        if (runtConfig is null)
        {
            return NotFound();
        }

        var result = await proxy.ExecuteAsync(runtConfig, queryType, query.Trim(), ct);
        return Results.Ok(new
        {
            provider = result.Provider,
            failoverReason = result.FailoverReason,
            latencyMs = result.LatencyMs,
            data = result.Payload
        });
    }

    private static IResult NotFound() =>
        Results.Json(new { code = ApiErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound);

    private static IResult Forbidden() =>
        Results.Json(new { code = ApiErrorCodes.Forbidden }, statusCode: StatusCodes.Status403Forbidden);

    private static IResult ValidationError(string message) =>
        Results.Json(new { code = ApiErrorCodes.ValidationError, message }, statusCode: StatusCodes.Status400BadRequest);
}
