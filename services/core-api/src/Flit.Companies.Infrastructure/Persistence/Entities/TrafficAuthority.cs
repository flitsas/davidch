namespace Flit.Companies.Infrastructure.Persistence.Entities;

public class TrafficAuthority
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Region { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CompanyTrafficAuthorityMatrix> CompanyMatrix { get; set; } = [];
}
