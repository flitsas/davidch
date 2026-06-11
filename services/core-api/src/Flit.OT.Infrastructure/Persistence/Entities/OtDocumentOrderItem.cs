using Flit.Identity.Infrastructure.Persistence.Entities;

namespace Flit.OT.Infrastructure.Persistence.Entities;

public class OtDocumentOrderItem
{
    public Guid TenantId { get; set; }
    public string ProcedureTypeCode { get; set; } = "";
    public string DocumentTypeCode { get; set; } = "";
    public int Position { get; set; }
    public bool IsIncluded { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public Tenant? Tenant { get; set; }
    public ProcedureTypeCatalog? ProcedureType { get; set; }
    public DocumentTypeCatalog? DocumentType { get; set; }
}
