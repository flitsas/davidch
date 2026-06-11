using Flit.Identity.Infrastructure.Tenancy;
using Flit.OT.Admin.Config;
using Microsoft.AspNetCore.Http;

namespace Flit.OT.Admin.Settings;

public sealed class OtSettingsHandler(
    OtIntegrationHandler integration,
    OtDocumentOrderHandler documentOrder,
    ITenantContext tenantContext)
{
    private Guid RequireTenantId() =>
        tenantContext.CurrentTenantId
        ?? throw new InvalidOperationException("Tenant context is required.");

    public Task<IResult> GetIntegrationAsync(CancellationToken ct) =>
        integration.GetForTenantAsync(RequireTenantId(), ct);

    public Task<IResult> PutIntegrationAsync(PutIntegrationRequest request, CancellationToken ct) =>
        integration.PutForTenantAsync(RequireTenantId(), request, ct);

    public Task<IResult> GetProcedureTypesAsync(CancellationToken ct) =>
        documentOrder.GetProcedureTypesAsync(ct);

    public Task<IResult> GetDocumentOrderAsync(string procedureCode, CancellationToken ct) =>
        documentOrder.GetForTenantAsync(RequireTenantId(), procedureCode, ct);

    public Task<IResult> PutDocumentOrderAsync(
        string procedureCode,
        PutDocumentOrderRequest request,
        Guid? updatedBy,
        CancellationToken ct) =>
        documentOrder.PutForTenantAsync(RequireTenantId(), procedureCode, request, updatedBy, ct);
}
