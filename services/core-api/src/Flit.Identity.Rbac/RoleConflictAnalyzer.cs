using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Domain;

namespace Flit.Identity.Rbac;

public sealed class RoleConflictAnalyzer
{
    public IReadOnlyList<RoleConflictWarning> Analyze(
        IReadOnlyList<PermissionGrant> current,
        IReadOnlyList<PermissionGrant> proposed)
    {
        var warnings = new List<RoleConflictWarning>();
        warnings.AddRange(DetectGrantRedundancies(proposed));
        return warnings;
    }

    public IReadOnlyList<RoleConflictWarning> AnalyzeRoles(IReadOnlyList<RoleGrantSet> roles)
    {
        var warnings = new List<RoleConflictWarning>();

        for (var i = 0; i < roles.Count; i++)
        {
            for (var j = 0; j < roles.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var roleA = roles[i];
                var roleB = roles[j];

                if (IsPermissionSubset(roleA.Permissions, roleB.Permissions))
                {
                    warnings.Add(new RoleConflictWarning(
                        "redundancy",
                        $"Role '{roleA.Name}' permissions are a subset of role '{roleB.Name}'.",
                        RoleId: roleA.Id,
                        OtherRoleId: roleB.Id));
                }
            }
        }

        warnings.AddRange(DetectCrossRoleScopeConflicts(roles));
        return warnings;
    }

    private static IEnumerable<RoleConflictWarning> DetectGrantRedundancies(IReadOnlyList<PermissionGrant> grants)
    {
        foreach (var group in grants.GroupBy(g => g.Key))
        {
            var scopes = group.Select(g => g.Scope).Distinct().ToList();
            if (scopes.Count <= 1)
            {
                continue;
            }

            var broadest = scopes.MaxBy(ScopeRank)!;
            foreach (var scope in scopes)
            {
                if (scope == broadest)
                {
                    continue;
                }

                if (ScopeSubsumes(broadest, scope))
                {
                    yield return new RoleConflictWarning(
                        "redundancy",
                        $"Permission '{group.Key}' with scope '{scope}' is redundant when '{broadest}' is also granted.",
                        PermissionKey: group.Key);
                }
            }
        }
    }

    private static IEnumerable<RoleConflictWarning> DetectCrossRoleScopeConflicts(
        IReadOnlyList<RoleGrantSet> roles)
    {
        var grantsByKey = new Dictionary<string, List<(Guid RoleId, string RoleName, PermissionScope Scope)>>();

        foreach (var role in roles)
        {
            foreach (var grant in role.Permissions)
            {
                if (!grantsByKey.TryGetValue(grant.Key, out var entries))
                {
                    entries = [];
                    grantsByKey[grant.Key] = entries;
                }

                if (!entries.Any(e => e.RoleId == role.Id && e.Scope == grant.Scope))
                {
                    entries.Add((role.Id, role.Name, grant.Scope));
                }
            }
        }

        foreach (var (key, entries) in grantsByKey)
        {
            var byRole = entries.GroupBy(e => e.RoleId).ToList();
            if (byRole.Count <= 1)
            {
                continue;
            }

            var scopes = entries.Select(e => e.Scope).Distinct().ToList();
            if (scopes.Count <= 1)
            {
                continue;
            }

            var roleA = byRole[0].First();
            var roleB = byRole[1].First();
            yield return new RoleConflictWarning(
                "scope_conflict",
                $"Roles '{roleA.RoleName}' and '{roleB.RoleName}' grant '{key}' with incompatible scopes ({roleA.Scope}, {roleB.Scope}).",
                RoleId: roleA.RoleId,
                OtherRoleId: roleB.RoleId,
                PermissionKey: key);
        }
    }

    private static bool IsPermissionSubset(
        IReadOnlyList<PermissionGrant> subset,
        IReadOnlyList<PermissionGrant> superset)
    {
        if (subset.Count == 0 || subset.Count >= superset.Count)
        {
            return false;
        }

        return subset.All(s =>
            superset.Any(g => g.Key == s.Key && ScopeSubsumes(g.Scope, s.Scope)));
    }

    private static int ScopeRank(PermissionScope scope) => scope switch
    {
        PermissionScope.Global => 3,
        PermissionScope.Tenant => 2,
        PermissionScope.Own => 1,
        _ => 0
    };

    private static bool ScopeSubsumes(PermissionScope broader, PermissionScope narrower) =>
        ScopeRank(broader) >= ScopeRank(narrower);
}

public record RoleGrantSet(Guid Id, string Name, IReadOnlyList<PermissionGrant> Permissions);

public record RoleConflictWarning(
    string Type,
    string Message,
    Guid? RoleId = null,
    Guid? OtherRoleId = null,
    string? PermissionKey = null);
