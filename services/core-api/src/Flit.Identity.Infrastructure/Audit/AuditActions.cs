namespace Flit.Identity.Infrastructure.Audit;

public static class AuditActions
{
    public const string LoginFailure = "auth.login_failure";
    public const string SuperAdminBypass = "auth.super_admin_bypass";
    public const string UserRolesChanged = "user.roles_changed";
    public const string UserBlocked = "user.blocked";
    public const string UserForceReset = "user.force_reset";
    public const string RolePermissionsChanged = "role.permissions_changed";
    public const string RoleMigrated = "role.migrated";
    public const string RoleDeleted = "role.deleted";
}
