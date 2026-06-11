using Flit.OT.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flit.OT.Infrastructure.Persistence.Seed;

public static class OtDbSeeder
{
    public static async Task SeedAsync(OtDbContext db, CancellationToken ct = default)
    {
        await SeedProcedureTypesAsync(db, ct);
        await SeedDocumentTypesAsync(db, ct);
        await SeedProcedureDocumentDefaultsAsync(db, ct);
    }

    private static async Task SeedProcedureTypesAsync(OtDbContext db, CancellationToken ct)
    {
        if (await db.ProcedureTypeCatalog.AnyAsync(ct))
        {
            return;
        }

        db.ProcedureTypeCatalog.AddRange(
            new ProcedureTypeCatalog { Code = "MATRICULA_INICIAL", Name = "Matrícula inicial" },
            new ProcedureTypeCatalog { Code = "TRASPASO", Name = "Traspaso de propiedad" },
            new ProcedureTypeCatalog { Code = "RADICADO_CUENTA", Name = "Radicado de cuenta" });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedDocumentTypesAsync(OtDbContext db, CancellationToken ct)
    {
        if (await db.DocumentTypeCatalog.AnyAsync(ct))
        {
            return;
        }

        db.DocumentTypeCatalog.AddRange(
            new DocumentTypeCatalog { Code = "CEDULA", Name = "Cédula", DefaultSortOrder = 1 },
            new DocumentTypeCatalog { Code = "TARJETA_PROPIEDAD", Name = "Tarjeta de propiedad", DefaultSortOrder = 2 },
            new DocumentTypeCatalog { Code = "SOAT", Name = "SOAT", DefaultSortOrder = 3 },
            new DocumentTypeCatalog { Code = "RTM", Name = "RTM", DefaultSortOrder = 4 },
            new DocumentTypeCatalog { Code = "FACTURA", Name = "Factura", DefaultSortOrder = 5 });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedProcedureDocumentDefaultsAsync(OtDbContext db, CancellationToken ct)
    {
        if (await db.ProcedureDocumentDefaults.AnyAsync(ct))
        {
            return;
        }

        var defaults = new List<ProcedureDocumentDefault>
        {
            Default("MATRICULA_INICIAL", "CEDULA", 1),
            Default("MATRICULA_INICIAL", "TARJETA_PROPIEDAD", 2),
            Default("MATRICULA_INICIAL", "SOAT", 3),
            Default("MATRICULA_INICIAL", "RTM", 4),
            Default("MATRICULA_INICIAL", "FACTURA", 5),
            Default("TRASPASO", "CEDULA", 1),
            Default("TRASPASO", "TARJETA_PROPIEDAD", 2),
            Default("TRASPASO", "FACTURA", 3),
            Default("RADICADO_CUENTA", "CEDULA", 1),
            Default("RADICADO_CUENTA", "TARJETA_PROPIEDAD", 2),
            Default("RADICADO_CUENTA", "SOAT", 3),
            Default("RADICADO_CUENTA", "RTM", 4),
        };

        db.ProcedureDocumentDefaults.AddRange(defaults);
        await db.SaveChangesAsync(ct);
    }

    private static ProcedureDocumentDefault Default(string procedureCode, string documentCode, int position) =>
        new()
        {
            ProcedureTypeCode = procedureCode,
            DocumentTypeCode = documentCode,
            DefaultPosition = position,
        };
}
