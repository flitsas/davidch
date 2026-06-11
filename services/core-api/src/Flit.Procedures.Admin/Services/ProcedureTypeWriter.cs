using Flit.Procedures.Infrastructure.Persistence;
using Flit.Procedures.Infrastructure.Persistence.Entities;
using Flit.Procedures.Shared;
using Flit.Procedures.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Admin.Services;

public sealed class ProcedureTypeWriter(
    ProceduresDbContext db,
    IProcedureCatalogSync catalogSync)
{
    public async Task<ProcedureType> SaveAsync(
        ProcedureType entity,
        IReadOnlyList<(string RoleLabel, int SortOrder)> actors,
        IReadOnlyList<(string Label, DocumentKind Kind, int SortOrder)> documents,
        CancellationToken ct)
    {
        if (actors.Count < 1)
        {
            throw new ArgumentException("At least one actor is required.", nameof(actors));
        }

        var now = DateTimeOffset.UtcNow;
        entity.UpdatedAt = now;
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = now;
            db.ProcedureTypes.Add(entity);
        }
        else
        {
            var existingActors = await db.ProcedureTypeActors
                .Where(a => a.ProcedureTypeId == entity.Id)
                .ToListAsync(ct);
            db.ProcedureTypeActors.RemoveRange(existingActors);

            var existingDocuments = await db.ProcedureTypeDocuments
                .Where(d => d.ProcedureTypeId == entity.Id)
                .ToListAsync(ct);
            db.ProcedureTypeDocuments.RemoveRange(existingDocuments);
        }

        foreach (var (roleLabel, sortOrder) in actors)
        {
            db.ProcedureTypeActors.Add(new ProcedureTypeActor
            {
                Id = Guid.NewGuid(),
                ProcedureTypeId = entity.Id,
                RoleLabel = roleLabel.Trim(),
                SortOrder = sortOrder,
            });
        }

        foreach (var (label, kind, sortOrder) in documents)
        {
            db.ProcedureTypeDocuments.Add(new ProcedureTypeDocument
            {
                Id = Guid.NewGuid(),
                ProcedureTypeId = entity.Id,
                Label = label.Trim(),
                Kind = kind,
                SortOrder = sortOrder,
            });
        }

        await db.SaveChangesAsync(ct);

        if (entity.IsActive)
        {
            await catalogSync.UpsertAsync(entity.Code, entity.Name, ct);
        }

        return entity;
    }
}
