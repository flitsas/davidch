using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.OT.Shared.Domain;

namespace Flit.OT.Infrastructure.Persistence.Entities;

public class OtProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string DivipolCode { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public IntegrationMode IntegrationMode { get; set; } = IntegrationMode.Dashboard;
    public OtStatus Status { get; set; } = OtStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Tenant? Tenant { get; set; }
    public ICollection<OtDocumentOrderItem> DocumentOrderItems { get; set; } = [];
}
