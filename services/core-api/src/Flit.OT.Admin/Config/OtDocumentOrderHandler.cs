using Flit.Identity.Shared.Errors;
using Flit.OT.Infrastructure.Persistence;
using Flit.OT.Infrastructure.Persistence.Entities;
using Flit.OT.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.OT.Admin.Config;

public sealed class OtDocumentOrderHandler(OtDbContext db)
{
    public async Task<IResult> GetForProfileAsync(Guid otId, string procedureCode, CancellationToken ct)
    {
        var profile = await db.OtProfiles.AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == otId, ct);

        if (profile is null)
        {
            return NotFound();
        }

        return await GetForTenantAsync(profile.TenantId, procedureCode, ct);
    }

    public async Task<IResult> PutForProfileAsync(
        Guid otId,
        string procedureCode,
        PutDocumentOrderRequest request,
        Guid? updatedBy,
        CancellationToken ct)
    {
        var profile = await db.OtProfiles.AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == otId, ct);

        if (profile is null)
        {
            return NotFound();
        }

        return await PutForTenantAsync(profile.TenantId, procedureCode, request, updatedBy, ct);
    }

    public async Task<IResult> GetForTenantAsync(Guid tenantId, string procedureCode, CancellationToken ct)
    {
        var normalizedCode = procedureCode.Trim();
        if (!await db.ProcedureTypeCatalog.AnyAsync(p => p.Code == normalizedCode, ct))
        {
            return NotFound();
        }

        var items = await LoadOrderResponsesAsync(tenantId, normalizedCode, ct);
        if (items.Count == 0)
        {
            return NotFound();
        }

        return Results.Ok(new DocumentOrderResponse(normalizedCode, items));
    }

    public async Task<IResult> PutForTenantAsync(
        Guid tenantId,
        string procedureCode,
        PutDocumentOrderRequest request,
        Guid? updatedBy,
        CancellationToken ct)
    {
        var normalizedCode = procedureCode.Trim();
        var validation = await ValidatePutAsync(tenantId, normalizedCode, request, ct);
        if (validation is not null)
        {
            return validation;
        }

        var now = DateTimeOffset.UtcNow;
        var existing = await db.OtDocumentOrderItems
            .Where(i => i.TenantId == tenantId && i.ProcedureTypeCode == normalizedCode)
            .ToDictionaryAsync(i => i.DocumentTypeCode, ct);

        foreach (var input in request.Items)
        {
            var code = input.DocumentTypeCode.Trim();
            if (!existing.TryGetValue(code, out var row))
            {
                continue;
            }

            row.Position = input.Position;
            row.IsIncluded = input.IsIncluded;
            row.UpdatedAt = now;
            row.UpdatedBy = updatedBy;
        }

        await db.SaveChangesAsync(ct);

        var items = await LoadOrderResponsesAsync(tenantId, normalizedCode, ct);
        return Results.Ok(new DocumentOrderResponse(normalizedCode, items));
    }

    public async Task<IResult> GetProcedureTypesAsync(CancellationToken ct)
    {
        var types = await db.ProcedureTypeCatalog.AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new ProcedureTypeSummary(p.Code, p.Name))
            .ToListAsync(ct);

        return Results.Ok(types);
    }

    private async Task<IReadOnlyList<DocumentOrderItemResponse>> LoadOrderResponsesAsync(
        Guid tenantId,
        string procedureCode,
        CancellationToken ct)
    {
        return await (
            from item in db.OtDocumentOrderItems.AsNoTracking()
            join doc in db.DocumentTypeCatalog.AsNoTracking()
                on item.DocumentTypeCode equals doc.Code
            where item.TenantId == tenantId && item.ProcedureTypeCode == procedureCode
            orderby item.Position, item.DocumentTypeCode
            select new DocumentOrderItemResponse(
                item.DocumentTypeCode,
                doc.Name,
                item.Position,
                item.IsIncluded)
        ).ToListAsync(ct);
    }

    private async Task<IResult?> ValidatePutAsync(
        Guid tenantId,
        string procedureCode,
        PutDocumentOrderRequest request,
        CancellationToken ct)
    {
        if (request.Items.Count == 0)
        {
            return ValidationError("At least one document item is required.");
        }

        if (!await db.ProcedureTypeCatalog.AnyAsync(p => p.Code == procedureCode, ct))
        {
            return NotFound();
        }

        var allowedCodes = await db.ProcedureDocumentDefaults.AsNoTracking()
            .Where(d => d.ProcedureTypeCode == procedureCode)
            .Select(d => d.DocumentTypeCode)
            .ToHashSetAsync(ct);

        if (allowedCodes.Count == 0)
        {
            return ValidationError("Procedure has no configured document types.");
        }

        var payloadCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.DocumentTypeCode))
            {
                return ValidationError("Document type code is required.");
            }

            var code = item.DocumentTypeCode.Trim();
            if (!allowedCodes.Contains(code))
            {
                return ValidationError($"Document type '{code}' is not valid for procedure '{procedureCode}'.");
            }

            if (!payloadCodes.Add(code))
            {
                return ValidationError($"Duplicate document type '{code}'.");
            }
        }

        if (!payloadCodes.SetEquals(allowedCodes))
        {
            return ValidationError("Payload must include all document types configured for the procedure.");
        }

        var included = request.Items
            .Where(i => i.IsIncluded)
            .OrderBy(i => i.Position)
            .ToList();

        for (var index = 0; index < included.Count; index++)
        {
            if (included[index].Position != index + 1)
            {
                return ValidationError("Included items must have contiguous positions starting at 1.");
            }
        }

        if (!await db.OtDocumentOrderItems.AnyAsync(
                i => i.TenantId == tenantId && i.ProcedureTypeCode == procedureCode,
                ct))
        {
            return NotFound();
        }

        return null;
    }

    private static IResult NotFound() =>
        Results.Json(new { code = ApiErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound);

    private static IResult ValidationError(string message) =>
        Results.Json(new { code = ApiErrorCodes.ValidationError, message }, statusCode: StatusCodes.Status400BadRequest);
}

public sealed class OtDocumentOrderService(OtDbContext db) : IOtDocumentOrderService
{
    public async Task<IReadOnlyList<DocumentOrderItemDto>> GetIncludedOrderAsync(
        Guid tenantId,
        string procedureTypeCode,
        CancellationToken ct)
    {
        return await db.OtDocumentOrderItems.AsNoTracking()
            .Where(i =>
                i.TenantId == tenantId
                && i.ProcedureTypeCode == procedureTypeCode
                && i.IsIncluded)
            .OrderBy(i => i.Position)
            .Select(i => new DocumentOrderItemDto(i.DocumentTypeCode, i.Position))
            .ToListAsync(ct);
    }
}
