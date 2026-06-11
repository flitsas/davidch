using Flit.OT.Infrastructure.Persistence;
using Flit.OT.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flit.OT.Infrastructure.Persistence;

public static class OtDefaultOrderFactory
{
    public static async Task SeedOrderItemsAsync(
        OtDbContext db,
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var defaults = await db.ProcedureDocumentDefaults.AsNoTracking().ToListAsync(ct);

        foreach (var item in defaults)
        {
            db.OtDocumentOrderItems.Add(new OtDocumentOrderItem
            {
                TenantId = tenantId,
                ProcedureTypeCode = item.ProcedureTypeCode,
                DocumentTypeCode = item.DocumentTypeCode,
                Position = item.DefaultPosition,
                IsIncluded = true,
                UpdatedAt = now
            });
        }
    }
}
