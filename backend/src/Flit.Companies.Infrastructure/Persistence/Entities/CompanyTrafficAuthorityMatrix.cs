namespace Flit.Companies.Infrastructure.Persistence.Entities;

public class CompanyTrafficAuthorityMatrix
{
    public Guid CompanyId { get; set; }
    public string AuthorityCode { get; set; } = "";
    public bool IsEnabled { get; set; }

    public Company? Company { get; set; }
    public TrafficAuthority? Authority { get; set; }
}
