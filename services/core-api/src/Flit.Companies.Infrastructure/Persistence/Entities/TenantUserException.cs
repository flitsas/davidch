using Flit.Identity.Infrastructure.Persistence.Entities;

namespace Flit.Companies.Infrastructure.Persistence.Entities;

public class TenantUserException
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }

    public Tenant? Tenant { get; set; }
    public User? User { get; set; }
}
