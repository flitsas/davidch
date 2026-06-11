namespace Flit.Procedures.Infrastructure.Persistence.Entities;

public class ProcedureTypeActor
{
    public Guid Id { get; set; }
    public Guid ProcedureTypeId { get; set; }
    public string RoleLabel { get; set; } = "";
    public int SortOrder { get; set; }

    public ProcedureType ProcedureType { get; set; } = null!;
}
