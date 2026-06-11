using Flit.OT.Shared.Domain;

namespace Flit.OT.Shared;

public interface IOtIntegrationModeService
{
    Task<IntegrationMode> GetModeAsync(Guid tenantId, CancellationToken ct);
}
