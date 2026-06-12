using Flit.Companies.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Shared.Errors;
using Flit.OT.Infrastructure.Persistence;
using Flit.OT.Infrastructure.Persistence.Entities;
using Flit.OT.Shared.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.OT.Admin.Crud;

public sealed class OtCrudHandler(
    OtDbContext otDb,
    IdentityDbContext identityDb,
    CompaniesDbContext companiesDb)
{
    public async Task<IResult> GetDetailAsync(Guid id, CancellationToken ct)
    {
        var profile = await otDb.OtProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == id, ct);

        if (profile is null)
        {
            return NotFound();
        }

        var tenant = await identityDb.Tenants
            .AsNoTracking()
            .SingleAsync(t => t.Id == profile.TenantId, ct);

        return Results.Ok(ToDetail(profile, tenant));
    }

    public async Task<IResult> CreateAsync(CreateOtRequest request, CancellationToken ct)
    {
        var validation = ValidateCreate(request);
        if (validation is not null)
        {
            return validation;
        }

        var divipol = request.DivipolCode.Trim();
        if (!await companiesDb.TrafficAuthorities.AnyAsync(a => a.Code == divipol, ct))
        {
            return ValidationError(
                "El código DIVIPOL debe existir en el catálogo de organismos de tránsito (p. ej. 05001000 para Medellín).");
        }

        if (await otDb.OtProfiles.AnyAsync(o => o.DivipolCode == divipol, ct))
        {
            return Conflict("DIVIPOL code already registered.");
        }

        var now = DateTimeOffset.UtcNow;
        var tenant = await ResolveTenantForCreateAsync(request, now, ct);
        if (tenant is null)
        {
            return Results.Json(
                new { code = ApiErrorCodes.ValidationError, errors = new { tenant_id = new[] { "Tenant not found or inactive." } } },
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (await otDb.OtProfiles.AnyAsync(o => o.TenantId == tenant.Id, ct))
        {
            return Conflict("Tenant already has an OT profile.");
        }

        var profile = new OtProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            DivipolCode = divipol,
            DisplayName = request.DisplayName.Trim(),
            Status = request.Status,
            IntegrationMode = IntegrationMode.Dashboard,
            CreatedAt = now,
            UpdatedAt = now
        };

        otDb.OtProfiles.Add(profile);
        await OtDefaultOrderFactory.SeedOrderItemsAsync(otDb, tenant.Id, now, ct);

        tenant.Name = profile.DisplayName;
        tenant.IsActive = request.Status == OtStatus.Active;

        await identityDb.SaveChangesAsync(ct);
        await otDb.SaveChangesAsync(ct);

        return Results.Created(
            $"/api/v1/admin/ot/{profile.Id}",
            new OtCreatedResponse(profile.Id, profile.TenantId));
    }

    public async Task<IResult> UpdateAsync(Guid id, UpdateOtRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DivipolCode) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return ValidationError("DIVIPOL code and display name are required.");
        }

        var profile = await otDb.OtProfiles.SingleOrDefaultAsync(o => o.Id == id, ct);
        if (profile is null)
        {
            return NotFound();
        }

        var divipol = request.DivipolCode.Trim();
        if (!await companiesDb.TrafficAuthorities.AnyAsync(a => a.Code == divipol, ct))
        {
            return ValidationError(
                "El código DIVIPOL debe existir en el catálogo de organismos de tránsito (p. ej. 05001000 para Medellín).");
        }

        if (await otDb.OtProfiles.AnyAsync(o => o.DivipolCode == divipol && o.Id != id, ct))
        {
            return Conflict("DIVIPOL code already registered.");
        }

        var tenant = await identityDb.Tenants.SingleAsync(t => t.Id == profile.TenantId, ct);
        profile.DivipolCode = divipol;
        profile.DisplayName = request.DisplayName.Trim();
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        tenant.Name = profile.DisplayName;

        await otDb.SaveChangesAsync(ct);
        await identityDb.SaveChangesAsync(ct);

        return Results.Ok(ToDetail(profile, tenant));
    }

    public async Task<IResult> UpdateStatusAsync(Guid id, UpdateOtStatusRequest request, CancellationToken ct)
    {
        var profile = await otDb.OtProfiles.SingleOrDefaultAsync(o => o.Id == id, ct);
        if (profile is null)
        {
            return NotFound();
        }

        var tenant = await identityDb.Tenants.SingleAsync(t => t.Id == profile.TenantId, ct);
        profile.Status = request.Status;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        tenant.IsActive = request.Status == OtStatus.Active;

        await otDb.SaveChangesAsync(ct);
        await identityDb.SaveChangesAsync(ct);

        return Results.Ok(ToDetail(profile, tenant));
    }

    private async Task<Tenant?> ResolveTenantForCreateAsync(
        CreateOtRequest request,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (request.Mode.Equals("link", StringComparison.OrdinalIgnoreCase))
        {
            if (request.TenantId is not { } tenantId)
            {
                return null;
            }

            return await identityDb.Tenants
                .SingleOrDefaultAsync(t => t.Id == tenantId && t.IsActive, ct);
        }

        if (!request.Mode.Equals("create", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            return null;
        }

        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await identityDb.Tenants.AnyAsync(t => t.Slug == slug, ct))
        {
            return null;
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.DisplayName.Trim(),
            Slug = slug,
            IsActive = request.Status == OtStatus.Active,
            CreatedAt = now
        };
        identityDb.Tenants.Add(tenant);
        return tenant;
    }

    private static IResult? ValidateCreate(CreateOtRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Mode))
        {
            return ValidationError("Mode is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DivipolCode) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return ValidationError("DIVIPOL code and display name are required.");
        }

        if (request.Mode.Equals("create", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.Slug))
        {
            return ValidationError("Slug is required for create mode.");
        }

        if (request.Mode.Equals("link", StringComparison.OrdinalIgnoreCase)
            && request.TenantId is null)
        {
            return ValidationError("Tenant id is required for link mode.");
        }

        if (!request.Mode.Equals("create", StringComparison.OrdinalIgnoreCase)
            && !request.Mode.Equals("link", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationError("Mode must be create or link.");
        }

        return null;
    }

    private static OtDetailResponse ToDetail(OtProfile profile, Tenant tenant) =>
        new(
            profile.Id,
            profile.TenantId,
            profile.DivipolCode,
            profile.DisplayName,
            profile.Status,
            profile.IntegrationMode,
            profile.CreatedAt,
            profile.UpdatedAt,
            new OtTenantSummary(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive));

    private static IResult NotFound() =>
        Results.Json(new { code = ApiErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound);

    private static IResult Conflict(string message) =>
        Results.Json(new { code = ApiErrorCodes.Conflict, message }, statusCode: StatusCodes.Status409Conflict);

    private static IResult ValidationError(string message) =>
        Results.Json(new { code = ApiErrorCodes.ValidationError, message }, statusCode: StatusCodes.Status400BadRequest);
}
