using Flit.Companies.Shared.Domain;

namespace Flit.Companies.Infrastructure.Persistence.Entities;

public class CompanyRuntConfig
{
    public Guid CompanyId { get; set; }
    public RuntProvider PrimaryProvider { get; set; } = RuntProvider.Verifik;
    public RuntProvider SecondaryProvider { get; set; } = RuntProvider.Intempo;
    public int FailoverTimeoutMs { get; set; } = 4000;
    public string ProviderCredentials { get; set; } = "{}";
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public Company? Company { get; set; }
}
