using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Infrastructure.Persistence.Entities;

public class ProcedureType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public VehicleQueryMode VehicleQueryMode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<ProcedureTypeActor> Actors { get; set; } = [];
    public ICollection<ProcedureTypeDocument> Documents { get; set; } = [];
}
