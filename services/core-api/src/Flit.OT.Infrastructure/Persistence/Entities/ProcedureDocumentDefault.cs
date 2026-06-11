namespace Flit.OT.Infrastructure.Persistence.Entities;

public class ProcedureDocumentDefault
{
    public string ProcedureTypeCode { get; set; } = "";
    public string DocumentTypeCode { get; set; } = "";
    public int DefaultPosition { get; set; }

    public ProcedureTypeCatalog? ProcedureType { get; set; }
    public DocumentTypeCatalog? DocumentType { get; set; }
}
