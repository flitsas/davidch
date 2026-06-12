using Flit.Identity.Shared.Errors;
using Flit.Procedures.Admin.Services;
using Flit.Procedures.Infrastructure.Persistence;
using Flit.Procedures.Infrastructure.Persistence.Entities;
using Flit.Procedures.Shared;
using Flit.Procedures.Shared.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Admin.Crud;

public sealed class ProcedureTypeCrudHandler(
    ProceduresDbContext db,
    ProcedureTypeWriter writer,
    IProcedureCatalogSync catalogSync)
{
    public async Task<IResult> GetDetailAsync(Guid id, CancellationToken ct)
    {
        var entity = await LoadDetailQuery().SingleOrDefaultAsync(p => p.Id == id, ct);
        return entity is null ? NotFound() : Results.Ok(ToDetail(entity));
    }

    public async Task<IResult> CreateAsync(SaveProcedureTypeRequest request, Guid? updatedBy, CancellationToken ct)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        var name = request.Name.Trim();
        if (await db.ProcedureTypes.AnyAsync(p => p.Name == name, ct))
        {
            return Conflict("PROCEDURE_NAME_DUPLICATE", "A procedure type with this name already exists.");
        }

        string code;
        try
        {
            code = ProcedureCodeGenerator.FromName(name);
        }
        catch (ArgumentException ex)
        {
            return ValidationError(ex.Message);
        }

        if (await db.ProcedureTypes.AnyAsync(p => p.Code == code, ct))
        {
            return Conflict("PROCEDURE_CODE_DUPLICATE", "Generated code already exists; choose a different name.");
        }

        var entity = new ProcedureType
        {
            Name = name,
            Code = code,
            VehicleQueryMode = request.VehicleQueryMode,
            IsActive = true,
            UpdatedBy = updatedBy,
        };

        try
        {
            await writer.SaveAsync(
                entity,
                MapActors(request.Actors),
                MapDocuments(request.Documents),
                ct);
        }
        catch (DbUpdateException)
        {
            return Conflict("PROCEDURE_NAME_DUPLICATE", "A procedure type with this name already exists.");
        }

        return Results.Created(
            $"/api/v1/admin/procedure-types/{entity.Id}",
            new ProcedureTypeCreatedResponse(entity.Id, entity.Code));
    }

    public async Task<IResult> UpdateAsync(
        Guid id,
        SaveProcedureTypeRequest request,
        Guid? updatedBy,
        CancellationToken ct)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        var entity = await db.ProcedureTypes.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();
        if (await db.ProcedureTypes.AnyAsync(p => p.Name == name && p.Id != id, ct))
        {
            return Conflict("PROCEDURE_NAME_DUPLICATE", "A procedure type with this name already exists.");
        }

        entity.Name = name;
        entity.VehicleQueryMode = request.VehicleQueryMode;
        entity.UpdatedBy = updatedBy;

        try
        {
            await writer.SaveAsync(
                entity,
                MapActors(request.Actors),
                MapDocuments(request.Documents),
                ct);
        }
        catch (DbUpdateException)
        {
            return Conflict("PROCEDURE_NAME_DUPLICATE", "A procedure type with this name already exists.");
        }

        var reloaded = await LoadDetailQuery().SingleAsync(p => p.Id == id, ct);
        return Results.Ok(ToDetail(reloaded));
    }

    public async Task<IResult> UpdateStatusAsync(
        Guid id,
        UpdateProcedureTypeStatusRequest request,
        Guid? updatedBy,
        CancellationToken ct)
    {
        var entity = await db.ProcedureTypes.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        if (request.IsActive && !entity.IsActive)
        {
            if (await db.ProcedureTypes.AnyAsync(
                    p => p.Name == entity.Name && p.IsActive && p.Id != id, ct))
            {
                return Conflict(
                    "PROCEDURE_NAME_DUPLICATE",
                    "An active procedure type with this name already exists.");
            }
        }

        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = updatedBy;
        await db.SaveChangesAsync(ct);

        if (entity.IsActive)
        {
            await catalogSync.UpsertAsync(entity.Code, entity.Name, ct);
        }

        var reloaded = await LoadDetailQuery().SingleAsync(p => p.Id == id, ct);
        return Results.Ok(ToDetail(reloaded));
    }

    private IQueryable<ProcedureType> LoadDetailQuery() =>
        db.ProcedureTypes
            .AsNoTracking()
            .Include(p => p.Actors.OrderBy(a => a.SortOrder))
            .Include(p => p.Documents.OrderBy(d => d.SortOrder));

    private static IResult? ValidateRequest(SaveProcedureTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationError("Name is required.");
        }

        if (request.Actors is null || request.Actors.Count < 1)
        {
            return ValidationError("At least one actor is required.");
        }

        if (request.Actors.Any(a => string.IsNullOrWhiteSpace(a.RoleLabel)))
        {
            return ValidationError("Each actor must have a role label.");
        }

        if (!Enum.IsDefined(request.VehicleQueryMode))
        {
            return ValidationError("Invalid vehicle query mode.");
        }

        if (request.Documents is not null)
        {
            foreach (var doc in request.Documents)
            {
                if (string.IsNullOrWhiteSpace(doc.Label))
                {
                    return ValidationError("Each document must have a label.");
                }

                if (!Enum.IsDefined(doc.Kind))
                {
                    return ValidationError("Invalid document kind.");
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<(string RoleLabel, int SortOrder)> MapActors(
        IReadOnlyList<ActorInputDto> actors) =>
        actors
            .Select((a, index) => (a.RoleLabel.Trim(), index + 1))
            .ToList();

    private static IReadOnlyList<(string Label, DocumentKind Kind, int SortOrder)> MapDocuments(
        IReadOnlyList<DocumentInputDto>? documents) =>
        (documents ?? [])
            .Select((d, index) => (d.Label.Trim(), d.Kind, index + 1))
            .ToList();

    private static ProcedureTypeDetailDto ToDetail(ProcedureType entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.Code,
            entity.VehicleQueryMode,
            entity.IsActive,
            entity.Actors
                .OrderBy(a => a.SortOrder)
                .Select(a => new ActorDetailDto(a.RoleLabel, a.SortOrder))
                .ToList(),
            entity.Documents
                .OrderBy(d => d.SortOrder)
                .Select(d => new DocumentDetailDto(d.Label, d.Kind, d.SortOrder))
                .ToList(),
            entity.CreatedAt,
            entity.UpdatedAt);

    private static IResult NotFound() =>
        Results.Json(new { code = ApiErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound);

    private static IResult ValidationError(string message) =>
        Results.Json(
            new { code = ApiErrorCodes.ValidationError, error = message },
            statusCode: StatusCodes.Status400BadRequest);

    private static IResult Conflict(string code, string message) =>
        Results.Json(
            new { code, error = message },
            statusCode: StatusCodes.Status409Conflict);
}
