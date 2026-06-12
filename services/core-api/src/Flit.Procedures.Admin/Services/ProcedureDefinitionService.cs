using Flit.Procedures.Infrastructure.Persistence;
using Flit.Procedures.Infrastructure.Persistence.Entities;
using Flit.Procedures.Shared;
using Flit.Procedures.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Admin.Services;

public sealed class ProcedureDefinitionService(ProceduresDbContext db) : IProcedureDefinitionService
{
    public async Task<ProcedureDefinitionDto?> GetByIdAsync(Guid procedureTypeId, CancellationToken ct)
    {
        var entity = await db.ProcedureTypes
            .AsNoTracking()
            .Include(p => p.Actors.OrderBy(a => a.SortOrder))
            .Include(p => p.Documents.OrderBy(d => d.SortOrder))
            .SingleOrDefaultAsync(p => p.Id == procedureTypeId && p.IsActive, ct);

        return entity is null ? null : ToDefinitionDto(entity);
    }

    public async Task<ProcedureDefinitionDto?> GetByCodeAsync(string procedureTypeCode, CancellationToken ct)
    {
        var entity = await db.ProcedureTypes
            .AsNoTracking()
            .Include(p => p.Actors.OrderBy(a => a.SortOrder))
            .Include(p => p.Documents.OrderBy(d => d.SortOrder))
            .SingleOrDefaultAsync(p => p.Code == procedureTypeCode && p.IsActive, ct);

        return entity is null ? null : ToDefinitionDto(entity);
    }

    public async Task<IReadOnlyList<ProcedureTypeSummaryDto>> ListActiveAsync(CancellationToken ct)
    {
        var items = await db.ProcedureTypes
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProcedureTypeSummaryDto(
                p.Id,
                p.Name,
                p.Code,
                p.VehicleQueryMode,
                p.IsActive,
                p.Actors.Count,
                p.Documents.Count,
                p.UpdatedAt))
            .ToListAsync(ct);

        return items;
    }

    internal static ProcedureDefinitionDto ToDefinitionDto(ProcedureType entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.Code,
            entity.VehicleQueryMode,
            entity.IsActive,
            entity.Actors
                .OrderBy(a => a.SortOrder)
                .Select(a => new ActorDefinitionDto(a.RoleLabel, a.SortOrder))
                .ToList(),
            entity.Documents
                .OrderBy(d => d.SortOrder)
                .Select(d => new DocumentDefinitionDto(d.Label, d.Kind, d.SortOrder))
                .ToList());
}
