namespace Flit.Identity.Shared.Auth;

public record CurrentUser(
    Guid Id,
    Guid? TenantId,
    bool IsSuperAdmin,
    int TokenVersion,
    IReadOnlyList<PermissionGrant> Permissions);
