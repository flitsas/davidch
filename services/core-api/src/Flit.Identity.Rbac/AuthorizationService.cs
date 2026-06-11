using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Domain;

namespace Flit.Identity.Rbac;

public sealed class AuthorizationService
{
    public bool CanAccess(CurrentUser user, string permissionKey, ResourceContext? resource = null)
    {
        if (user.IsSuperAdmin)
        {
            return true;
        }

        var grants = user.Permissions.Where(p => p.Key == permissionKey).ToList();
        if (grants.Count == 0)
        {
            return false;
        }

        foreach (var grant in grants)
        {
            switch (grant.Scope)
            {
                case PermissionScope.Global:
                    return true;
                case PermissionScope.Tenant:
                    if (resource is not null && resource.TenantId == user.TenantId)
                    {
                        return true;
                    }

                    break;
                case PermissionScope.Own:
                    if (resource is not null && resource.TenantId == user.TenantId
                        && resource.OwnerId == user.Id)
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;
    }
}
