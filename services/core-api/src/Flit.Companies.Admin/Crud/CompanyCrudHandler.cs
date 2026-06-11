using Flit.Companies.Infrastructure.Persistence;
using Flit.Companies.Infrastructure.Persistence.Entities;
using Flit.Companies.Shared.Domain;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Companies.Admin.Crud;

public sealed class CompanyCrudHandler(
    CompaniesDbContext companiesDb,
    IdentityDbContext identityDb)
{
    public async Task<IResult> GetDetailAsync(Guid id, CancellationToken ct)
    {
        var company = await companiesDb.Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, ct);

        if (company is null)
        {
            return Results.Json(
                new { code = ApiErrorCodes.NotFound },
                statusCode: StatusCodes.Status404NotFound);
        }

        var tenant = await identityDb.Tenants
            .AsNoTracking()
            .SingleAsync(t => t.Id == company.TenantId, ct);

        return Results.Ok(ToDetail(company, tenant));
    }

    public async Task<IResult> CreateAsync(CreateCompanyRequest request, CancellationToken ct)
    {
        var validation = ValidateCreate(request);
        if (validation is not null)
        {
            return validation;
        }

        if (await companiesDb.Companies.AnyAsync(c => c.Nit == request.Nit.Trim(), ct))
        {
            return Conflict("NIT already registered.");
        }

        var now = DateTimeOffset.UtcNow;
        var authorityCodes = await companiesDb.TrafficAuthorities
            .AsNoTracking()
            .Select(a => a.Code)
            .ToListAsync(ct);

        var tenant = await ResolveTenantForCreateAsync(request, now, ct);
        if (tenant is null)
        {
            return Results.Json(
                new { code = ApiErrorCodes.ValidationError, errors = new { tenant_id = new[] { "Tenant not found or inactive." } } },
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (await companiesDb.Companies.AnyAsync(c => c.TenantId == tenant.Id, ct))
        {
            return Conflict("Tenant already has a company profile.");
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Nit = request.Nit.Trim(),
            LegalName = request.LegalName.Trim(),
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        CompanyDefaultConfigFactory.SeedDefaults(company, authorityCodes, now);
        companiesDb.Companies.Add(company);

        tenant.Name = company.LegalName;
        tenant.IsActive = request.Status == CompanyStatus.Active;

        await identityDb.SaveChangesAsync(ct);
        await companiesDb.SaveChangesAsync(ct);

        return Results.Created(
            $"/api/v1/admin/companies/{company.Id}",
            new CompanyCreatedResponse(company.Id, company.TenantId));
    }

    public async Task<IResult> UpdateAsync(Guid id, UpdateCompanyRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Nit) || string.IsNullOrWhiteSpace(request.LegalName))
        {
            return ValidationError("NIT and legal name are required.");
        }

        var company = await companiesDb.Companies
            .SingleOrDefaultAsync(c => c.Id == id, ct);

        if (company is null)
        {
            return NotFound();
        }

        var nit = request.Nit.Trim();
        if (await companiesDb.Companies.AnyAsync(c => c.Nit == nit && c.Id != id, ct))
        {
            return Conflict("NIT already registered.");
        }

        var tenant = await identityDb.Tenants.SingleAsync(t => t.Id == company.TenantId, ct);
        company.Nit = nit;
        company.LegalName = request.LegalName.Trim();
        company.UpdatedAt = DateTimeOffset.UtcNow;
        tenant.Name = company.LegalName;

        await companiesDb.SaveChangesAsync(ct);
        await identityDb.SaveChangesAsync(ct);

        return Results.Ok(ToDetail(company, tenant));
    }

    public async Task<IResult> UpdateStatusAsync(
        Guid id,
        UpdateCompanyStatusRequest request,
        CancellationToken ct)
    {
        var company = await companiesDb.Companies
            .SingleOrDefaultAsync(c => c.Id == id, ct);

        if (company is null)
        {
            return NotFound();
        }

        var tenant = await identityDb.Tenants.SingleAsync(t => t.Id == company.TenantId, ct);
        company.Status = request.Status;
        company.UpdatedAt = DateTimeOffset.UtcNow;
        tenant.IsActive = request.Status == CompanyStatus.Active;

        await companiesDb.SaveChangesAsync(ct);
        await identityDb.SaveChangesAsync(ct);

        return Results.Ok(ToDetail(company, tenant));
    }

    private async Task<Tenant?> ResolveTenantForCreateAsync(
        CreateCompanyRequest request,
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
            Name = request.LegalName.Trim(),
            Slug = slug,
            IsActive = request.Status == CompanyStatus.Active,
            CreatedAt = now
        };
        identityDb.Tenants.Add(tenant);
        return tenant;
    }

    private static IResult? ValidateCreate(CreateCompanyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Mode))
        {
            return ValidationError("Mode is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Nit) || string.IsNullOrWhiteSpace(request.LegalName))
        {
            return ValidationError("NIT and legal name are required.");
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

    private static CompanyDetailResponse ToDetail(Company company, Tenant tenant) =>
        new(
            company.Id,
            company.TenantId,
            company.Nit,
            company.LegalName,
            company.Status,
            company.CreatedAt,
            company.UpdatedAt,
            new TenantSummary(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive));

    private static IResult NotFound() =>
        Results.Json(new { code = ApiErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound);

    private static IResult Conflict(string message) =>
        Results.Json(new { code = ApiErrorCodes.Conflict, message }, statusCode: StatusCodes.Status409Conflict);

    private static IResult ValidationError(string message) =>
        Results.Json(new { code = ApiErrorCodes.ValidationError, message }, statusCode: StatusCodes.Status400BadRequest);
}
