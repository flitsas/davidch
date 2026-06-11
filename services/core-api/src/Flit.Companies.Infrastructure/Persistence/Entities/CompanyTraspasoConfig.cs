namespace Flit.Companies.Infrastructure.Persistence.Entities;

public class CompanyTraspasoConfig
{
    public Guid CompanyId { get; set; }
    public bool OnlyOwnVehicles { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public Company? Company { get; set; }
}
