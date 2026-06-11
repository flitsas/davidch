using Flit.Identity.Shared.Domain;

namespace Flit.Identity.Shared.Auth;

public record PermissionGrant(string Key, PermissionScope Scope);
