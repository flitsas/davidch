using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Infrastructure.Persistence.Entities;

public class ProcedureInstance
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProcedureTypeId { get; set; }
    public string ProcedureTypeCode { get; set; } = "";
    public Guid OtTenantId { get; set; }
    public string OtDivipolCode { get; set; } = "";
    public string VehicleQueryValue { get; set; } = "";
    public ProcedureStatus Status { get; set; } = ProcedureStatus.PendienteEnvio;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ProcedureType ProcedureType { get; set; } = null!;
    public ICollection<ProcedureInstanceActor> Actors { get; set; } = [];
    public ICollection<ProcedureInstanceDocument> Documents { get; set; } = [];
}
