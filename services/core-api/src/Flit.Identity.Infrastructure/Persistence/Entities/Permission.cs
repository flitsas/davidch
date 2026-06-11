using Flit.Identity.Shared.Domain;

namespace Flit.Identity.Infrastructure.Persistence.Entities;

public class Permission
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";
    public PermissionType Type { get; set; }
    public string? Module { get; set; }
    public string Description { get; set; } = "";
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
