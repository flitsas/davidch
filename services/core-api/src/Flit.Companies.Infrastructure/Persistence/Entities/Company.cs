using Flit.Companies.Shared.Domain;
using Flit.Identity.Infrastructure.Persistence.Entities;

namespace Flit.Companies.Infrastructure.Persistence.Entities;

public class Company
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Nit { get; set; } = "";
    public string LegalName { get; set; } = "";
    public CompanyStatus Status { get; set; } = CompanyStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Tenant? Tenant { get; set; }
    public CompanyMatriculaConfig? MatriculaConfig { get; set; }
    public CompanyTraspasoConfig? TraspasoConfig { get; set; }
    public CompanySignatureConfig? SignatureConfig { get; set; }
    public CompanyNotificationConfig? NotificationConfig { get; set; }
    public CompanyPaymentConfig? PaymentConfig { get; set; }
    public CompanyRuntConfig? RuntConfig { get; set; }
    public ICollection<CompanyTrafficAuthorityMatrix> TrafficAuthorityMatrix { get; set; } = [];
}
