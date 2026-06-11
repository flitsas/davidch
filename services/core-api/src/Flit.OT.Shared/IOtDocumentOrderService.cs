using Flit.OT.Shared;

namespace Flit.OT.Shared;

public interface IOtDocumentOrderService
{
    Task<IReadOnlyList<DocumentOrderItemDto>> GetIncludedOrderAsync(
        Guid tenantId,
        string procedureTypeCode,
        CancellationToken ct);
}
