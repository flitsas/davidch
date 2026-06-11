namespace Flit.OT.Infrastructure.Persistence.Entities;

public class DocumentTypeCatalog
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int DefaultSortOrder { get; set; }

    public ICollection<ProcedureDocumentDefault> ProcedureDefaults { get; set; } = [];
    public ICollection<OtDocumentOrderItem> DocumentOrderItems { get; set; } = [];
}
