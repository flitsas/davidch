using Flit.Companies.Shared.Domain;

namespace Flit.Companies.Infrastructure.Persistence.Entities;

public class RuntProviderCatalog
{
    public RuntProvider Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
