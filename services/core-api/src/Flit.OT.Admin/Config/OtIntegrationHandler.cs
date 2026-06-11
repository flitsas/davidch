using Flit.Identity.Shared.Errors;
using Flit.OT.Admin.Config;
using Flit.OT.Infrastructure.Persistence;
using Flit.OT.Infrastructure.Persistence.Entities;
using Flit.OT.Shared;
using Flit.OT.Shared.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.OT.Admin.Config;

public sealed class OtIntegrationHandler(OtDbContext db)
{
    public async Task<IResult> GetForProfileAsync(Guid otId, CancellationToken ct)
    {
        var profile = await FindProfileByIdAsync(otId, ct);
        if (profile is null)
        {
            return NotFound();
        }

        return Results.Ok(ToDto(profile));
    }

    public async Task<IResult> PutForProfileAsync(
        Guid otId,
        PutIntegrationRequest request,
        CancellationToken ct)
    {
        var profile = await db.OtProfiles.SingleOrDefaultAsync(o => o.Id == otId, ct);
        if (profile is null)
        {
            return NotFound();
        }

        profile.IntegrationMode = request.IntegrationMode;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.Ok(ToDto(profile));
    }

    public async Task<IResult> GetForTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var profile = await FindProfileByTenantAsync(tenantId, ct);
        if (profile is null)
        {
            return NotFound();
        }

        return Results.Ok(ToDto(profile));
    }

    public async Task<IResult> PutForTenantAsync(
        Guid tenantId,
        PutIntegrationRequest request,
        CancellationToken ct)
    {
        var profile = await db.OtProfiles.SingleOrDefaultAsync(o => o.TenantId == tenantId, ct);
        if (profile is null)
        {
            return NotFound();
        }

        profile.IntegrationMode = request.IntegrationMode;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.Ok(ToDto(profile));
    }

    private async Task<OtProfile?> FindProfileByIdAsync(Guid otId, CancellationToken ct) =>
        await db.OtProfiles.AsNoTracking().SingleOrDefaultAsync(o => o.Id == otId, ct);

    private async Task<OtProfile?> FindProfileByTenantAsync(Guid tenantId, CancellationToken ct) =>
        await db.OtProfiles.AsNoTracking().SingleOrDefaultAsync(o => o.TenantId == tenantId, ct);

    private static IntegrationConfigDto ToDto(OtProfile profile) =>
        new() { IntegrationMode = profile.IntegrationMode };

    private static IResult NotFound() =>
        Results.Json(new { code = ApiErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound);
}

public sealed class OtIntegrationModeService(OtDbContext db) : IOtIntegrationModeService
{
    public async Task<IntegrationMode> GetModeAsync(Guid tenantId, CancellationToken ct)
    {
        var profile = await db.OtProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(o => o.TenantId == tenantId, ct);

        if (profile is null)
        {
            throw new InvalidOperationException($"No OT profile for tenant {tenantId}.");
        }

        return profile.IntegrationMode;
    }
}
