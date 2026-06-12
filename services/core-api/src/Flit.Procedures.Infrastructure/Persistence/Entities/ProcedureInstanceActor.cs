using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Infrastructure.Persistence.Entities;

public class ProcedureInstanceActor
{
    public Guid Id { get; set; }
    public Guid ProcedureInstanceId { get; set; }
    public string RoleLabel { get; set; } = "";
    public int SortOrder { get; set; }
    public PersonKind PersonKind { get; set; }
    public DocumentIdType DocumentType { get; set; }
    public string DocumentNumber { get; set; } = "";
    public bool IsLegalRepresentative { get; set; }
    public Guid? ParentActorId { get; set; }
    public string? ExternalDataJson { get; set; }

    public ProcedureInstance ProcedureInstance { get; set; } = null!;
    public ProcedureInstanceActor? ParentActor { get; set; }
}
