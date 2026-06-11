namespace Flit.Companies.Infrastructure.Persistence.Entities;

public class CompanyMatriculaConfig
{
    public Guid CompanyId { get; set; }
    public bool AllowNewVehicleFiling { get; set; }
    public bool AllowMiscProcedures { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public Company? Company { get; set; }
}
