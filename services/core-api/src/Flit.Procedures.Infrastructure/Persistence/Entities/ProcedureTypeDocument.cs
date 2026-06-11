using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Infrastructure.Persistence.Entities;

public class ProcedureTypeDocument
{
    public Guid Id { get; set; }
    public Guid ProcedureTypeId { get; set; }
    public string Label { get; set; } = "";
    public DocumentKind Kind { get; set; }
    public int SortOrder { get; set; }

    public ProcedureType ProcedureType { get; set; } = null!;
}
