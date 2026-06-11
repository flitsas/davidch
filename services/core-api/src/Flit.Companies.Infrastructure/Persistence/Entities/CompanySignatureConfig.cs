using Flit.Companies.Shared.Domain;

namespace Flit.Companies.Infrastructure.Persistence.Entities;

public class CompanySignatureConfig
{
    public Guid CompanyId { get; set; }
    public SignatureType SellerSignatureType { get; set; }
    public SignatureType BuyerSignatureType { get; set; }
    public bool VaultEnabled { get; set; }
    public string? VaultSettings { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public Company? Company { get; set; }
}
