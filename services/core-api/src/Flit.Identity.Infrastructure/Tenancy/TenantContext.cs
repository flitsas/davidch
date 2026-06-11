namespace Flit.Identity.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public Guid? UserId { get; set; }
    public Guid? CurrentTenantId { get; set; }
    public bool IsSuperAdmin { get; set; }
    public int TokenVersion { get; set; }
}
