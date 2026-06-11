using Flit.Identity.Rbac.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Flit.Identity.Rbac;

public static class RbacEndpoints
{
    public static IEndpointRouteBuilder MapRbacEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPermissionsEndpoints();
        app.MapRolesEndpoints();
        return app;
    }
}
