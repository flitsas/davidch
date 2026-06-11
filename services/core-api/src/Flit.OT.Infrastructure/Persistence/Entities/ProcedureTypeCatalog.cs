namespace Flit.OT.Infrastructure.Persistence.Entities;

public class ProcedureTypeCatalog
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public ICollection<ProcedureDocumentDefault> DocumentDefaults { get; set; } = [];
    public ICollection<OtDocumentOrderItem> DocumentOrderItems { get; set; } = [];
}
