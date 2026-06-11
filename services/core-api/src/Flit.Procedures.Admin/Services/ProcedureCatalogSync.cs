using Flit.OT.Infrastructure.Persistence;
using Flit.OT.Infrastructure.Persistence.Entities;
using Flit.Procedures.Shared;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Admin.Services;

public sealed class ProcedureCatalogSync(OtDbContext otDb) : IProcedureCatalogSync
{
    public async Task UpsertAsync(string code, string name, CancellationToken ct)
    {
        var existing = await otDb.ProcedureTypeCatalog.SingleOrDefaultAsync(p => p.Code == code, ct);
        if (existing is null)
        {
            otDb.ProcedureTypeCatalog.Add(new ProcedureTypeCatalog { Code = code, Name = name });
        }
        else
        {
            existing.Name = name;
        }

        await otDb.SaveChangesAsync(ct);
    }
}
