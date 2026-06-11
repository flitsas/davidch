using Flit.Identity.Rbac;
using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Domain;

namespace Flit.Identity.UnitTests.Rbac;

public class AuthorizationServiceTests
{
    private readonly AuthorizationService _svc = new();

    [Fact]
    public void SuperAdmin_bypasses_all_checks()
    {
        var user = new CurrentUser(Guid.NewGuid(), null, true, 1, []);
        Assert.True(_svc.CanAccess(user, "tramites:read", new ResourceContext(Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public void Own_scope_requires_matching_owner()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var user = new CurrentUser(userId, tenantId, false, 1,
            [new PermissionGrant("tramites:read", PermissionScope.Own)]);

        Assert.True(_svc.CanAccess(user, "tramites:read",
            new ResourceContext(tenantId, userId)));
        Assert.False(_svc.CanAccess(user, "tramites:read",
            new ResourceContext(tenantId, Guid.NewGuid())));
    }

    [Fact]
    public void Multi_role_union_allows_if_any_role_grants()
    {
        var tenantId = Guid.NewGuid();
        var user = new CurrentUser(Guid.NewGuid(), tenantId, false, 1,
        [
            new PermissionGrant("tramites:read", PermissionScope.Own),
            new PermissionGrant("tramites:read", PermissionScope.Tenant)
        ]);
        Assert.True(_svc.CanAccess(user, "tramites:read",
            new ResourceContext(tenantId, Guid.NewGuid())));
    }
}
