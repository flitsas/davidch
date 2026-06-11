using Flit.Identity.Shared.Domain;

namespace Flit.Identity.Infrastructure.Persistence.Entities;

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public PermissionScope Scope { get; set; }
}
