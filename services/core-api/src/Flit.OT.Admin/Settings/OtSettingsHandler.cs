using Flit.Identity.Infrastructure.Tenancy;
using Flit.OT.Admin.Config;
using Microsoft.AspNetCore.Http;

namespace Flit.OT.Admin.Settings;

public sealed class OtSettingsHandler(OtIntegrationHandler integration, ITenantContext tenantContext)
{
    public Task<IResult> GetIntegrationAsync(CancellationToken ct)
    {
        var tenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required.");
        return integration.GetForTenantAsync(tenantId, ct);
    }

    public Task<IResult> PutIntegrationAsync(PutIntegrationRequest request, CancellationToken ct)
    {
        var tenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required.");
        return integration.PutForTenantAsync(tenantId, request, ct);
    }
}
