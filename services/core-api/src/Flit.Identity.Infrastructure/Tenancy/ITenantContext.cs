namespace Flit.Identity.Infrastructure.Tenancy;

public interface ITenantContext
{
    Guid? UserId { get; set; }
    Guid? CurrentTenantId { get; set; }
    bool IsSuperAdmin { get; set; }
    int TokenVersion { get; set; }
}
