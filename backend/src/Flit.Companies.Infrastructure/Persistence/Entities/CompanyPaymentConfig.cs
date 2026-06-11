namespace Flit.Companies.Infrastructure.Persistence.Entities;

public class CompanyPaymentConfig
{
    public Guid CompanyId { get; set; }
    public bool AllowFlitGateway { get; set; }
    public bool AllowOt { get; set; }
    public bool AllowOther { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public Company? Company { get; set; }
}
