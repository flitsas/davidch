using Flit.Identity.Rbac;
using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Domain;

namespace Flit.Identity.UnitTests.Rbac;

public class RoleConflictAnalyzerTests
{
    private readonly RoleConflictAnalyzer _analyzer = new();

    [Fact]
    public void Detects_redundant_role_when_permissions_subset()
    {
        var warnings = _analyzer.Analyze(
            current: [new PermissionGrant("tramites:read", PermissionScope.Own)],
            proposed:
            [
                new PermissionGrant("tramites:read", PermissionScope.Own),
                new PermissionGrant("tramites:read", PermissionScope.Tenant)
            ]);

        Assert.Contains(warnings, w => w.Type == "redundancy");
    }

    [Fact]
    public void Detects_redundant_role_when_one_role_permissions_subset_of_another()
    {
        var viewer = new RoleGrantSet(
            Guid.NewGuid(),
            "Viewer",
            [new PermissionGrant("tramites:read", PermissionScope.Own)]);

        var editor = new RoleGrantSet(
            Guid.NewGuid(),
            "Editor",
            [
                new PermissionGrant("tramites:read", PermissionScope.Own),
                new PermissionGrant("tramites:update", PermissionScope.Tenant)
            ]);

        var warnings = _analyzer.AnalyzeRoles([viewer, editor]);

        Assert.Contains(warnings, w =>
            w.Type == "redundancy" && w.Message.Contains("Viewer") && w.Message.Contains("Editor"));
    }

    [Fact]
    public void Detects_scope_conflict_when_roles_grant_same_key_with_incompatible_scopes()
    {
        var roleA = new RoleGrantSet(
            Guid.NewGuid(),
            "RoleA",
            [new PermissionGrant("tramites:read", PermissionScope.Own)]);

        var roleB = new RoleGrantSet(
            Guid.NewGuid(),
            "RoleB",
            [new PermissionGrant("tramites:read", PermissionScope.Tenant)]);

        var warnings = _analyzer.AnalyzeRoles([roleA, roleB]);

        Assert.Contains(warnings, w => w.Type == "scope_conflict" && w.PermissionKey == "tramites:read");
    }
}
